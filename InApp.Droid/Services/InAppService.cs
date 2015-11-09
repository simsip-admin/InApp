using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using InApp.Models;
using InApp.Services;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.InAppBilling;
using Xamarin.InAppBilling.Utilities;


[assembly: Dependency(typeof(InApp.Droid.Services.InAppService))]

namespace InApp.Droid.Services
{
    public class InAppService : IInAppService
    {
        private InAppBillingServiceConnection _serviceConnection;
        private bool _connected;

        #region IInappService implementation
#if TEST_INAPP
        public string PracticeModeProductId { get { return ReservedTestProductIDs.Purchased; } }
        // public string PracticeModeProductId { get { return ReservedTestProductIDs.Canceled; } }
#else
        public string PracticeModeProductId { get { return "com.yourcompany.practicemode"; } }
#endif

        public void Initialize()
        {
            // A Licensing and In-App Billing public key is required before an app can communicate with
            // Google Play, however you DON'T want to store the key in plain text with the application.
            // The Unify command provides a simply way to obfuscate the key by breaking it into two or
            // or more parts, specifying the order to reassemlbe those parts and optionally providing
            // a set of key/value pairs to replace in the final string. 
            string value = Security.Unify(
                new string[] { 
                    "Insert part 0",
                    "Insert part 3",
                    "Insert part 2",
                    "Insert part 1" },
                new int[] { 0, 3, 2, 1 });

            // Create a new connection to the Google Play Service
            _serviceConnection = new InAppBillingServiceConnection(MainActivity.Instance, value);
            _serviceConnection.OnConnected += () =>
            {
                this._serviceConnection.BillingHandler.OnProductPurchased += (int response, Purchase purchase, string purchaseData, string purchaseSignature) =>
                {
                    // Record what we purchased
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var purchaseTime = epoch.AddMilliseconds(purchase.PurchaseTime);
                    var newPurchase = new InAppPurchase
                    {
                        OrderId = purchase.OrderId,
                        ProductId = purchase.ProductId,
                        PurchaseTime = purchaseTime
                    };
                    App.ViewModel.Purchases.Add(newPurchase);

                    // Let anyone know who is interested that purchase has completed
                    if (this.OnPurchaseProduct != null)
                    {
                        this.OnPurchaseProduct();
                    }
                };
                this._serviceConnection.BillingHandler.QueryInventoryError += (int responseCode, Bundle skuDetails) =>
                {
                    if (this.OnQueryInventoryError != null)
                    {
                        this.OnQueryInventoryError(responseCode, null);
                    }
                };
                this._serviceConnection.BillingHandler.BuyProductError += (int responseCode, string sku) =>
                {
                    // Note, BillingResult.ItemAlreadyOwned, etc. can be used to determine error

                    if (this.OnPurchaseProductError != null)
                    {
                        this.OnPurchaseProductError(responseCode, sku);
                    }
                };
                this._serviceConnection.BillingHandler.InAppBillingProcesingError += (string message) =>
                {
                    if (this.OnInAppBillingProcesingError != null)
                    {
                        this.OnInAppBillingProcesingError(message);
                    }
                };
                this._serviceConnection.BillingHandler.OnInvalidOwnedItemsBundleReturned += (Bundle ownedItems) =>
                {
                    if (this.OnInvalidOwnedItemsBundleReturned != null)
                    {
                        this.OnInvalidOwnedItemsBundleReturned(null);
                    }
                };
                this._serviceConnection.BillingHandler.OnProductPurchasedError += (int responseCode, string sku) =>
                {
                    if (this.OnPurchaseProductError != null)
                    {
                        this.OnPurchaseProductError(responseCode, sku);
                    }
                };
                this._serviceConnection.BillingHandler.OnPurchaseFailedValidation += (Purchase purchase, string purchaseData, string purchaseSignature) =>
                {
                    if (this.OnPurchaseFailedValidation != null)
                    {
                        this.OnPurchaseFailedValidation(null, purchaseData, purchaseSignature);
                    }
                };
                this._serviceConnection.BillingHandler.OnUserCanceled += () =>
                {
                    if (this.OnUserCanceled != null)
                    {
                        this.OnUserCanceled();
                    }
                };

                this._connected = true;

                // Load inventory or available products
                this.QueryInventory();
            };

            /* Uncomment these if you want to be notified for these events
            _serviceConnection.OnDisconnected += () =>
            {
                System.Diagnostics.Debug.WriteLine("Remove");
            };

            _serviceConnection.OnInAppBillingError += (error, message) =>
                {
                    System.Diagnostics.Debug.WriteLine("Remove");
                };
            */

            // Are we connected to a network?
            ConnectivityManager connectivityManager = (ConnectivityManager)MainActivity.Instance.GetSystemService(MainActivity.ConnectivityService);
            NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
            if ((activeConnection != null) && activeConnection.IsConnected)
            {
                // Ok, carefully attempt to connect to the in-app service
                try
                {

                    _serviceConnection.Connect();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception trying to connect to in app service: " + ex);
                }
            }
        }

        public void HandleActivityResult(int requestCode, Result resultCode, Intent data)
        {
            this._serviceConnection.BillingHandler.HandleActivityResult(requestCode, resultCode, data);
        }

        public async void QueryInventory()
        {
            var products = await this._serviceConnection.BillingHandler.QueryInventoryAsync(
                                new List<string>() 
                                    { 
                                        this.PracticeModeProductId
                                    }, 
                                    ItemType.Product);

            // Update inventory
            foreach (var product in products)
            {
                var newProduct = new InAppProduct();
                newProduct.ProductId = product.ProductId;
                newProduct.Type = ItemType.Product;
                newProduct.Price = product.Price;
                newProduct.Title = product.Title;
                newProduct.Description = product.Description;
                newProduct.PriceCurrencyCode = product.Price_Currency_Code;

                App.ViewModel.Products.Add(newProduct);
            }

            if (this.OnQueryInventory != null)
            {
                this.OnQueryInventory();
            }
        }

        public void PurchaseProduct(string productId)
        {
            // See Initialize() for where we hook up event handler for this
            this._serviceConnection.BillingHandler.BuyProduct(productId, ItemType.Product, "payload");
        }

        public void RestoreProducts()
        {
            var purchases = this._serviceConnection.BillingHandler.GetPurchases(ItemType.Product);

            // Record what we restored
            foreach (var purchase in purchases)
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var purchaseTime = epoch.AddMilliseconds(purchase.PurchaseTime);
                var newPurchase = new InAppPurchase
                {
                    OrderId = purchase.OrderId,
                    ProductId = purchase.ProductId,
                    PurchaseTime = purchaseTime
                };

                App.ViewModel.Purchases.Add(newPurchase);
            }

            // Notifiy anyone who needs to know products were restored
            if (this.OnRestoreProducts != null)
            {
                this.OnRestoreProducts();
            }
        }

        public void RefundProduct()
        {
            var purchases = this._serviceConnection.BillingHandler.GetPurchases(ItemType.Product);

            this._serviceConnection.BillingHandler.ConsumePurchase(purchases[0].PurchaseToken);
        }

        public void OnDestroy()
        {
            // Are we attached to the Google Play Service?
            if (this._serviceConnection != null)
            {
                // Yes, disconnect
                this._serviceConnection.Disconnect();
            }
        }

        public event OnQueryInventoryDelegate OnQueryInventory;

        public event OnPurchaseProductDelegate OnPurchaseProduct;

        public event OnRestoreProductsDelegate OnRestoreProducts;

        public event OnQueryInventoryErrorDelegate OnQueryInventoryError;

        public event OnPurchaseProductErrorDelegate OnPurchaseProductError;

        public event OnRestoreProductsErrorDelegate OnRestoreProductsError;

        public event OnUserCanceledDelegate OnUserCanceled;

        public event OnInAppBillingProcessingErrorDelegate OnInAppBillingProcesingError;

        public event OnInvalidOwnedItemsBundleReturnedDelegate OnInvalidOwnedItemsBundleReturned;

        public event OnPurchaseFailedValidationDelegate OnPurchaseFailedValidation;

        #endregion

    }
}