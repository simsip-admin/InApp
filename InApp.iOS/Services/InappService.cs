using Foundation;
using InApp.Models;
using InApp.Services;
using StoreKit;
using System;
using System.Diagnostics;
using Xamarin.Forms;


[assembly: Dependency(typeof(InApp.iOS.Services.InAppService))]

namespace InApp.iOS.Services
{
    public class InAppService : SKProductsRequestDelegate, IInAppService
    {
        private CustomPaymentObserver _customPaymentObserver;

        public static NSString InAppQueryInventoryNotification = new NSString("InAppQueryInventoryNotification");
        public static NSString InAppQueryInventoryErrorNotification = new NSString("InAppQueryInventoryErrorNotification");
        public static NSString InAppPurchaseProductNotification = new NSString("InAppPurchaseProductNotification");
        public static NSString InAppPurchaseProductErrorNotification = new NSString("InAppPurchaseProductErrorNotification");
        public static NSString InAppRestoreProductsNotification = new NSString("InAppRestoreProductsNotification");
        public static NSString InAppRestoreProductsErrorNotification = new NSString("InAppRestoreProductsErrorNotification");

        private NSObject _queryInventoryObserver;
        private NSObject _queryInventoryErrorObserver;
        private NSObject _purchaseProductObserver;
        private NSObject _purchaseProductErrorObserver;
        private NSObject _restoreProductsObserver;
        private NSObject _restoreProductsErrorObserver;

        private SKProductsRequest _productsRequest;

        #region SKProductsRequestDelegate overrides

        // Received response to RequestProductData - with price, title, description info
		public override void ReceivedResponse (SKProductsRequest request, SKProductsResponse response)
		{
			SKProduct[] products = response.Products;

			NSDictionary userInfo = null;
			if (products.Length > 0) 
            {
				NSObject[] productIdsArray = new NSObject[response.Products.Length];
				NSObject[] productsArray = new NSObject[response.Products.Length];
				for (int i = 0; i < response.Products.Length; i++) {
					productIdsArray[i] = new NSString(response.Products[i].ProductIdentifier);
					productsArray[i] = response.Products[i];
				}
				userInfo = NSDictionary.FromObjectsAndKeys (productsArray, productIdsArray);
			}
			NSNotificationCenter.DefaultCenter.PostNotificationName(
                InAppQueryInventoryNotification,
                this,
                userInfo);

			foreach (string invalidProductId in response.InvalidProducts) 
            {
				Debug.WriteLine("Invalid product id: " + invalidProductId );
			}
		}

        // Probably could not connect to the App Store (network unavailable?)
        public override void RequestFailed(SKRequest request, NSError error)
        {
            Debug.WriteLine(" ** InAppPurchaseManager RequestFailed() " + error.LocalizedDescription);
            using (var pool = new NSAutoreleasePool())
            {
                NSDictionary userInfo = NSDictionary.FromObjectsAndKeys(new NSObject[] { error }, new NSObject[] { new NSString("error") });
                // send out a notification for the failed transaction
                NSNotificationCenter.DefaultCenter.PostNotificationName(InAppQueryInventoryErrorNotification, this, userInfo);
            }
        }

        #endregion

        #region IInappService implementation

        public string PracticeModeProductId { get { return "com.yourcompany.practicemode"; } }

        public void Initialize()
        {
            this._customPaymentObserver = new CustomPaymentObserver(this);
            SKPaymentQueue.DefaultQueue.AddTransactionObserver(this._customPaymentObserver);

            this._queryInventoryObserver = NSNotificationCenter.DefaultCenter.AddObserver(InAppService.InAppQueryInventoryNotification,
                (notification) =>
                {
                    var info = notification.UserInfo;
                    if (info == null)
                    {
                        // TODO: Had to put this in so it wouldn't crash, needs a revisit
                        return;
                    }
                    var practiceModeProductId = new NSString(this.PracticeModeProductId);

                    var product = (SKProduct)info.ObjectForKey(practiceModeProductId);

                    // Update inventory
                    var newProduct = new InAppProduct();
                    newProduct.ProductId = this.PracticeModeProductId;
                    newProduct.Type = "inapp";
                    newProduct.Price = this.LocalizedPrice(product);
                    newProduct.PriceCurrencyCode = product.PriceLocale.CurrencyCode;
                    newProduct.Title = product.LocalizedTitle;
                    newProduct.Description = product.LocalizedDescription;

                    App.ViewModel.Products.Add(newProduct);

                    // Notify anyone who needed to know that our inventory is in
                    if (this.OnQueryInventory != null)
                    {
                        this.OnQueryInventory();
                    }
                });

            this._queryInventoryErrorObserver = NSNotificationCenter.DefaultCenter.AddObserver(InAppService.InAppQueryInventoryErrorNotification,
                (notification) =>
                {
                    // Notify anyone who needed to know that there was a query inventory error
                    if (this.OnQueryInventoryError != null)
                    {
                        this.OnQueryInventoryError(0, null);
                    }
                });

            this._purchaseProductObserver = NSNotificationCenter.DefaultCenter.AddObserver(InAppService.InAppPurchaseProductNotification,
                (notification) =>
                {
                    // Notify anyone who needed to know that product was purchased
                    if (this.OnPurchaseProduct != null)
                    {
                        this.OnPurchaseProduct();
                    }

                });

            this._purchaseProductErrorObserver = NSNotificationCenter.DefaultCenter.AddObserver(InAppService.InAppPurchaseProductErrorNotification,
                (notification) =>
                {
                    // Notify anyone who needed to know that there was a product purchase error
                    if (this.OnPurchaseProductError != null)
                    {
                        this.OnPurchaseProductError(0, string.Empty);
                    }
                });

            this._restoreProductsObserver = NSNotificationCenter.DefaultCenter.AddObserver(InAppService.InAppRestoreProductsNotification,
                (notification) =>
                {
                    // Notify anyone who needed to know that products were restored
                    if (this.OnRestoreProducts != null)
                    {
                        this.OnRestoreProducts();
                    }

                });

            this._restoreProductsErrorObserver = NSNotificationCenter.DefaultCenter.AddObserver(InAppService.InAppRestoreProductsErrorNotification,
                (notification) =>
                {
                    // Notify anyone who needed to know that there was an error in restoring products
                    if (this.OnRestoreProductsError != null)
                    {
                        this.OnRestoreProductsError(0, null);
                    }
                });

            if (this.CanMakePayments())
            {
                // Async request 
                // StoreKit -> App Store -> ReceivedResponse (see below)
                this.QueryInventory();
            }

        }

        public void QueryInventory()
        {
            var array = new NSString[1];
            array[0] = new NSString(this.PracticeModeProductId);
            NSSet productIdentifiers = NSSet.MakeNSObjectSet<NSString>(array);

            // Set up product request for in-app purchase to be handled in
            // SKProductsRequestDelegate.ReceivedResponse (see above)
            this._productsRequest = new SKProductsRequest(productIdentifiers);
            this._productsRequest.Delegate = this; 
            this._productsRequest.Start();
        }

        public void PurchaseProduct(string productId)
        {
            // Construct a payment request
            var payment = SKPayment.PaymentWithProduct(productId);

            // Queue the payment request up
            // Will be handled in:
            // CustomPaymentObserver.UpdatedTransactions -> InAppService.PurchaseTransaction - InAppService.FinishTransaction
            SKPaymentQueue.DefaultQueue.AddPayment(payment);
        }

        /// <summary>
        /// RestoreProducts any transactions that occurred for this Apple ID, either on 
        /// this device or any other logged in with that account.
        /// </summary>
        public void RestoreProducts()
        {
            Debug.WriteLine(" ** InAppPurchaseManager Restore()");
            // theObserver will be notified of when the restored transactions start arriving <- AppStore
            SKPaymentQueue.DefaultQueue.RestoreCompletedTransactions();
        }

        // TODO
        public void RefundProduct()
        {

        }

        public bool CanMakePayments()
        {
            return SKPaymentQueue.CanMakePayments;
        }

        public void WillTerminate()
        {
            // Remove the observer when the app exits
            NSNotificationCenter.DefaultCenter.RemoveObserver(this._queryInventoryObserver);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this._queryInventoryErrorObserver);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this._purchaseProductObserver);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this._purchaseProductErrorObserver);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this._restoreProductsObserver);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this._restoreProductsErrorObserver);
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

        #region Api

        public void PurchaseTransaction(SKPaymentTransaction transaction)
        {
            var productId = transaction.Payment.ProductIdentifier;

            // Record the purchase
            var newPurchase = new InAppPurchase
                {
                    OrderId = transaction.TransactionIdentifier,
                    ProductId = transaction.Payment.ProductIdentifier,
                    PurchaseTime = NSDateToDateTime(transaction.TransactionDate)
                };
            App.ViewModel.Purchases.Add(newPurchase);

            // Remove the transaction from the payment queue.
            // IMPORTANT: Let's ios know we're done
            SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);

            // Send out a notification that we’ve finished the transaction
            using (var pool = new NSAutoreleasePool())
            {
                NSDictionary userInfo = NSDictionary.FromObjectsAndKeys(new NSObject[] { transaction }, new NSObject[] { new NSString("transaction") });
                NSNotificationCenter.DefaultCenter.PostNotificationName(InAppPurchaseProductNotification, this, userInfo);
            }
        }

        public void RestoreTransaction(SKPaymentTransaction transaction)
        {
            // Restored Transactions always have an 'original transaction' attached
            var productId = transaction.OriginalTransaction.Payment.ProductIdentifier;

            // Record the restore
            var newPurchase = new InAppPurchase
                {
                    OrderId = transaction.OriginalTransaction.TransactionIdentifier,
                    ProductId = transaction.OriginalTransaction.Payment.ProductIdentifier,
                    PurchaseTime = NSDateToDateTime(transaction.OriginalTransaction.TransactionDate)
                };
            App.ViewModel.Purchases.Add(newPurchase);

            // Remove the transaction from the payment queue.
            // IMPORTANT: Let's ios know we're done
            SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);

            // Send out a notification that we’ve finished the transaction
            using (var pool = new NSAutoreleasePool())
            {
                NSDictionary userInfo = NSDictionary.FromObjectsAndKeys(new NSObject[] { transaction }, new NSObject[] { new NSString("transaction") });
                NSNotificationCenter.DefaultCenter.PostNotificationName(InAppRestoreProductsNotification, this, userInfo);
            }
        }

        public void FailedTransaction(SKPaymentTransaction transaction)
        {
            //SKErrorPaymentCancelled == 2
            if (transaction.Error.Code == 2) // user cancelled
            {
                Debug.WriteLine("User CANCELLED FailedTransaction Code=" + transaction.Error.Code + " " + transaction.Error.LocalizedDescription);
            }
            else // error!
            {
                Debug.WriteLine("FailedTransaction Code=" + transaction.Error.Code + " " + transaction.Error.LocalizedDescription);
            }

            // Remove the transaction from the payment queue.
            // IMPORTANT: Let's ios know we're done
            SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);
            
            // Send out a notification for the failed transaction
            using (var pool = new NSAutoreleasePool())
            {
                NSDictionary userInfo = NSDictionary.FromObjectsAndKeys(new NSObject[] { transaction }, new NSObject[] { new NSString("transaction") });
                NSNotificationCenter.DefaultCenter.PostNotificationName(InAppPurchaseProductErrorNotification, this, userInfo);
            }
        }

        #endregion

        #region Helper methods

        private string LocalizedPrice(SKProduct product)
        {
            var formatter = new NSNumberFormatter();
            formatter.FormatterBehavior = NSNumberFormatterBehavior.Version_10_4;
            formatter.NumberStyle = NSNumberFormatterStyle.Currency;
            formatter.Locale = product.PriceLocale;
            var formattedString = formatter.StringFromNumber(product.Price);
            return formattedString;
        }

        private DateTime NSDateToDateTime(NSDate date)
        {
            // NSDate has a wider range than DateTime, so clip
            // the converted date to DateTime.Min|MaxValue.
            double secs = date.SecondsSinceReferenceDate;
            if (secs < -63113904000)
                return DateTime.MinValue;
            if (secs > 252423993599)
                return DateTime.MaxValue;
            return (DateTime)date;
        }

        #endregion

    }

    internal class CustomPaymentObserver : SKPaymentTransactionObserver
    {
        private InAppService _inAppService;

        public CustomPaymentObserver(InAppService inAppService)
        {
            _inAppService = inAppService;
        }

        public override void UpdatedTransactions(SKPaymentQueue queue, SKPaymentTransaction[] transactions)
        {
            foreach (SKPaymentTransaction transaction in transactions)
            {
                switch (transaction.TransactionState)
                {
                    case SKPaymentTransactionState.Purchased:
                        this._inAppService.PurchaseTransaction(transaction);
                        break;
                    case SKPaymentTransactionState.Failed:
                        this._inAppService.FailedTransaction(transaction);
                        break;
                    case SKPaymentTransactionState.Restored:
                        this._inAppService.RestoreTransaction(transaction);
                        break;
                    default:
                        break;
                }
            }
        }

        public override void PaymentQueueRestoreCompletedTransactionsFinished(SKPaymentQueue queue)
        {
            // RestoreProducts succeeded
            Debug.WriteLine(" ** RESTORE PaymentQueueRestoreCompletedTransactionsFinished ");
        }

        public override void RestoreCompletedTransactionsFailedWithError(SKPaymentQueue queue, NSError error)
        {
            // RestoreProducts failed somewhere...
            Debug.WriteLine(" ** RESTORE RestoreCompletedTransactionsFailedWithError " + error.LocalizedDescription);
        }
    }
}