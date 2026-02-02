using UnityEngine;

public class FingerFreezeManager : MonoBehaviour
{
    private bool[,] _isFrozen = new bool[2, 6];
    private float[,] _savedCurls = new float[2, 5];
    private Quaternion[] _savedWristRot = new Quaternion[2];

    public void SetFreeze(bool left, int index, bool freeze)
    {
        if (index < 0 || index > 5) return;
        int hand = left ? 0 : 1;
        _isFrozen[hand, index] = freeze;

        if (freeze) CaptureCurrentState(left, index);
        Debug.Log($"[FreezeMgr] {(left ? "Sol" : "Sað")} el, index {index} = {freeze}");
    }

    void CaptureCurrentState(bool left, int index)
    {
        var bridge = GloveUnityBridge.Instance;
        if (bridge == null) return;
        int hand = left ? 0 : 1;

        if (index < 5)
        {
            float[] flex = left ? bridge.leftFlex : bridge.rightFlex;
            if (flex != null && flex.Length > index)
                _savedCurls[hand, index] = flex[index];
        }
        else
        {
            _savedWristRot[hand] = left ? bridge.leftWrist.rotation : bridge.rightWrist.rotation;
        }
    }

    public bool IsFingerFrozen(bool left, int fingerIndex)
    {
        if (fingerIndex < 0 || fingerIndex > 4) return false;
        return _isFrozen[left ? 0 : 1, fingerIndex];
    }

    public bool TryGetFrozenCurl(bool left, int fingerIndex, out float curl)
    {
        curl = 0f;
        if (fingerIndex < 0 || fingerIndex > 4) return false;
        if (_isFrozen[left ? 0 : 1, fingerIndex])
        {
            curl = _savedCurls[left ? 0 : 1, fingerIndex];
            return true;
        }
        return false;
    }

    public bool TryGetFrozenWristRot(bool left, out Quaternion rot)
    {
        rot = Quaternion.identity;
        int hand = left ? 0 : 1;
        if (_isFrozen[hand, 5])
        {
            rot = _savedWristRot[hand];
            return true;
        }
        return false;
    }

    // Butonlarýn baþlangýç durumunu senkronize etmek için yeni metod
    public bool GetCurrentFreezeState(bool left, int index)
    {
        if (index < 0 || index > 5) return false;
        return _isFrozen[left ? 0 : 1, index];
    }
}