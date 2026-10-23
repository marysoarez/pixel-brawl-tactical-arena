using PixelBrawl.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace PixelBrawl.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopItemButton : MonoBehaviour
    {
        [SerializeField] private ShopController shop;
        [SerializeField] private ShopItemData item;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statusText;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Buy);
        }

        private void OnEnable()
        {
            Refresh();
            shop.ItemsChanged += Refresh;
        }

        private void OnDisable()
        {
            shop.ItemsChanged -= Refresh;
        }

        private void Buy()
        {
            shop.TryPurchase(item);
            Refresh();
        }

        private void Refresh()
        {
            if (item == null || shop == null)
            {
                return;
            }

            nameText.text = $"{item.Category}  |  {item.DisplayName}";
            statusText.text = shop.GetStatus(item);
            button.interactable = shop.CanAttemptPurchase(item);
        }
    }
}
