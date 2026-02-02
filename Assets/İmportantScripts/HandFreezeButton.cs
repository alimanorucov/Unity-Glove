using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HandFreezeButton : MonoBehaviour
{
    public bool isLeftHand = true;
    [Range(0, 5)] public int index = 0;
    public Image checkmarkImage; // Bu iþaret, butonun "dondurmak için hazýr" olduðunu göstersin.

    private bool _isFrozen = false;
    private FingerFreezeManager _freezeManager;
    private Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
    }

    void Start()
    {
        _freezeManager = FindObjectOfType<FingerFreezeManager>();

        // 1. BUTON BAÞLANGIÇ DURUMUNU FREEZEMANAGER'DAN SENKRONÝZE ET
        if (_freezeManager != null)
        {
            _isFrozen = _freezeManager.GetCurrentFreezeState(isLeftHand, index);
        }

        // 2. TIKLAMA OLAYINI BAÐLA
        _btn.onClick.RemoveAllListeners();
        _btn.onClick.AddListener(OnButtonClicked);

        // 3. GÖRSELÝ ÝLK DURUMA GÖRE GÜNCELLE
        UpdateButtonVisual();
    }

    void OnButtonClicked()
    {
        // Durumu tersine çevir: Eðer donmamýþsa, dondur. Donmuþsa, serbest býrak.
        bool newFreezeState = !_isFrozen;

        // FreezeManager'a yeni durumu bildir
        if (_freezeManager != null)
        {
            _freezeManager.SetFreeze(isLeftHand, index, newFreezeState);
        }

        // Yerel durumu ve görseli güncelle
        _isFrozen = newFreezeState;
        UpdateButtonVisual();
    }

    void UpdateButtonVisual()
    {
        if (checkmarkImage != null)
        {
            // YENÝ VE DOÐRU MANTIK:
            // Eðer parmak DONMUÞSA (frozen), iþareti GÝZLE (çünkü artýk "dondur" modunda deðil).
            // Eðer serbestse (not frozen), iþareti GÖSTER (çünkü "dondurmak için týkla" anlamýnda).
            checkmarkImage.enabled = !_isFrozen;

            // Alternatif: Eðer farklý bir simge kullanmak isterseniz (örneðin kilit ikonu),
            // burada checkmarkImage.sprite deðiþtirebilirsiniz.
        }

        // Ýsteðe baðlý: Buton rengini de deðiþtirebilirsiniz.
        // Örneðin dondurulmuþsa kýrmýzý, serbestse yeþil yapmak gibi.
        // Image btnImage = _btn.GetComponent<Image>();
        // if (btnImage != null) btnImage.color = _isFrozen ? Color.red : Color.green;
    }

    // Opsiyonel: Dýþarýdan durum deðiþikliði yapýlýrsa (baþka bir script'ten)
    // bu metodu çaðýrarak butonu güncelleyebilirsiniz.
    public void SyncWithFreezeState()
    {
        if (_freezeManager != null)
        {
            _isFrozen = _freezeManager.GetCurrentFreezeState(isLeftHand, index);
            UpdateButtonVisual();
        }
    }
}