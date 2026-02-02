using System;
using UnityEngine;

public class GloveUnityBridge : MonoBehaviour
{
    public static GloveUnityBridge Instance { get; private set; }
    public float[] leftFlex = new float[5];
    public float[] rightFlex = new float[5];
    public Pose leftWrist = Pose.identity;
    public Pose rightWrist = Pose.identity;
    public bool connectedLeft, connectedRight;
    public event Action<GloveSnapshot> OnSnapshotReceived;

    // FREEZE MANAGER REFERANSI
    private FingerFreezeManager _freezeManager;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // FreezeManager'ý baþlangýçta bul
        _freezeManager = FindObjectOfType<FingerFreezeManager>();
    }

    void OnEnable() => GloveComm.OnSnapshot += HandleSnapshot;
    void OnDisable() => GloveComm.OnSnapshot -= HandleSnapshot;

    void HandleSnapshot(GloveSnapshot s)
    {
        if (s == null) return;

        // HANGÝ EL ÝÇÝN VERÝ GELDÝ?
        bool isLeft = (s.handedness != null && s.handedness.ToUpper().Contains("LEFT"));
        bool isRight = (s.handedness != null && s.handedness.ToUpper().Contains("RIGHT"));

        if (isLeft)
        {
            ApplySnapshotWithFreeze(true, s); // Sol el için iþle
            connectedLeft = true;
        }
        else if (isRight)
        {
            ApplySnapshotWithFreeze(false, s); // Sað el için iþle
            connectedRight = true;
        }

        OnSnapshotReceived?.Invoke(s);
    }

    // YENÝ METOD: Freeze durumuna göre veriyi uygula
    void ApplySnapshotWithFreeze(bool forLeftHand, GloveSnapshot snap)
    {
        float[] targetFlex = forLeftHand ? leftFlex : rightFlex;
        ref Pose targetWrist = ref (forLeftHand ? ref leftWrist : ref rightWrist);

        // 1. ÖNCE ham veriyi al (normalde yaptýðýmýz gibi)
        ApplyToArray(targetFlex, snap.flex);
        ApplyToPose(ref targetWrist, snap.wpos, snap.wrot);

        // 2. SONRA freeze durumunu KONTROL ET ve UYGULA
        if (_freezeManager != null)
        {
            // Bilek dondurulmuþ mu? (Sadece rotasyon)
            if (_freezeManager.TryGetFrozenWristRot(forLeftHand, out Quaternion frozenRot))
            {
                targetWrist.rotation = frozenRot;
            }

            // Parmaklar dondurulmuþ mu?
            for (int i = 0; i < 5; i++)
            {
                if (_freezeManager.IsFingerFrozen(forLeftHand, i))
                {
                    // Bu parmak dondurulmuþ! Mock veriyi DEÐÝL, kayýtlý deðeri kullan.
                    if (_freezeManager.TryGetFrozenCurl(forLeftHand, i, out float frozenCurl))
                    {
                        targetFlex[i] = frozenCurl; // Mock veriyi EZ!
                        Debug.Log($"[BRIDGE-FREEZE] {(forLeftHand ? "Sol" : "Sað")} el, parmak {i} MOCK'U EZDÝ. Deðer: {frozenCurl}");
                    }
                }
            }
        }
    }

    void ApplyToArray(float[] dst, float[] src)
    {
        if (dst == null || src == null) return;
        int n = Mathf.Min(dst.Length, src.Length);
        for (int i = 0; i < n; i++) dst[i] = Mathf.Clamp01(src[i]);
    }

    void ApplyToPose(ref Pose p, float[] pos, float[] rot)
    {
        if (pos != null && pos.Length >= 3)
            p.position = new Vector3(pos[0], pos[1], pos[2]);
        if (rot != null && rot.Length >= 4)
            p.rotation = new Quaternion(rot[0], rot[1], rot[2], rot[3]);
    }

    // Debug için: Bridge'deki deðerleri konsolda göster
    [ContextMenu("Debug Bridge Values")]
    void DebugValues()
    {
        Debug.Log("=== BRIDGE DEBUG ===");
        Debug.Log($"Sol el flex: {string.Join(", ", leftFlex)}");
        Debug.Log($"Sað el flex: {string.Join(", ", rightFlex)}");
    }
}