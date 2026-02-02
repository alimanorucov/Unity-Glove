using UnityEngine;

public class VGExternalControllerAdapter : MonoBehaviour
{
    public bool isLeftHand = true;
    private FingerFreezeManager _freezeManager;

    [HideInInspector] public float[] fingerFlexions = new float[5];
    [HideInInspector] public Vector3 sensorPosition = Vector3.zero;
    [HideInInspector] public Quaternion sensorRotation = Quaternion.identity;

    void Start()
    {
        _freezeManager = FindAnyObjectByType<FingerFreezeManager>();
    }

    void Update()
    {
        var bridge = GloveUnityBridge.Instance;
        if (bridge == null) return;

        float[] sourceFlex = isLeftHand ? bridge.leftFlex : bridge.rightFlex;
        Pose sourceWrist = isLeftHand ? bridge.leftWrist : bridge.rightWrist;
        if (sourceFlex == null) return;

        // Bilek Pozisyonu
        sensorPosition = sourceWrist.position;
        sensorRotation = sourceWrist.rotation;
        if (_freezeManager != null && _freezeManager.TryGetFrozenWristRot(isLeftHand, out Quaternion frozenRot))
            sensorRotation = frozenRot;

        // Parmak Deðerleri (KRÝTÝK DEBUG BLOÐU BURADA)
        for (int i = 0; i < 5; i++)
        {
            float curlValue = sourceFlex[i];
            bool wasFrozen = false;

            if (_freezeManager != null && _freezeManager.IsFingerFrozen(isLeftHand, i))
            {
                if (_freezeManager.TryGetFrozenCurl(isLeftHand, i, out float frozenCurl))
                {
                    curlValue = frozenCurl;
                    wasFrozen = true;
                }
            }

            // DEBUG SATIRI: Konsolda freeze uygulanýyor mu gör!
            if (wasFrozen)
            {
                Debug.Log($"[ADAPTER-DEBUG] {gameObject.name} - Parmak {i} DONDURULDU. Deðer: {curlValue}");
            }

            fingerFlexions[i] = curlValue;
        }
    }

    // VirtualGrasp'in arayacaðý metodlar
    public bool DoGetFingerFlexions(out float[] flexions)
    {
        flexions = fingerFlexions;
        return true;
    }

    public bool DoGetSensorPose(out Vector3 position, out Quaternion rotation)
    {
        position = sensorPosition;
        rotation = sensorRotation;
        return true;
    }
}