---
layout: home
title: Bill McRoberts
home: true
---

## Xamarin Forms Dependency Service for In-App Purchases

The challenge with cross-platform in-app purchasing (IAP) is that the transaction flows are so different across iOS and Android. In the game Linerunner 3D, I wanted a consistent interface to code against for IAP. While I was able to accomplish this, the code-base was shared code not PCL so I ended up with a lot of #defines. With Xamarin.Forms and the DependencyService we have a cleaner way of exposing platform-specific functionality exposed via a consistent API. This sample exposes platform specific IAP functionality via a single interface that can be consumed in a Xamarin.Forms PCL.  

<img src="images/screenshot-ios-inapp.PNG" width="40%">  <img src="images/screenshot-android-inapp.png" width="40%">


### Setup
1. In Visual Studio, start with a new Blank App (Xamarin.Forms.Portable), and remove the iOS and Windows Phone platform projects.
1. Head over to Fortumo ()[http://fortumo.com](http://fortumo.com "Fortumo")) and sign-up as a developer.
2. Download the Fortumo Android SDK jar.
3. In your Visual Studio solution you created above, create a new Bindings Library (Android) project.
1. Place the Fortumo Android SDK jar file in the resulting Jars folder.
2. Add the Fortumo Android SDK jar file to the project and set the Build Action to ```EmbeddedJar```.
3. Build the Bindings project and make sure there are no errors.

### The Interface
The interface exposes an API and set of events to consume IAP.

> But wait you say, can't we do this with a Task based API instead of hooking up events? Hang in there that's exactly where I want to evolve this sample in the future.

In your portable project create a Services folder and the interface ```IFortumoInAppService```.

Add the following API to the interface:

        string PracticeModeProductId { get; }

        /// <summary>
        /// Starts the setup of this Android application by connection to the Google Play Service
        /// to handle In-App purchases.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Queries the inventory asynchronously and returns a list of Xamarin.Android.InAppBilling.Products
        /// matching the given list of SKU numbers.
        /// </summary>
        /// <param name="skuList">Sku list.</param>
        /// <param name="itemType">The Xamarin.Android.InAppBilling.ItemType of product being queried.</param>
        /// <returns>List of Xamarin.Android.InAppBilling.Products matching the given list of SKUs.
        /// </returns>
        void QueryInventory();

        /// <summary>
        /// Buys the given Xamarin.Android.InAppBilling.Product
        /// 
        /// This method automatically generates a unique GUID and attaches it as the
        /// developer payload for this purchase.
        /// </summary>
        /// <param name="product">The Xamarin.Android.InAppBilling.Product representing the item the users
        /// wants to purchase.</param>
        void PurchaseProduct(string productId);

        void RestoreProducts();

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        void RefundProduct();

Add the following events to the interface:

        /// <summary>
        /// Occurs when a query inventory transactions completes successfully with Google Play Services.
        /// </summary>
        event OnQueryInventoryDelegate OnQueryInventory;

        /// <summary>
        /// Occurs after a product has been successfully purchased Google Play.
        /// 
        /// This event is fired after a OnProductPurchased which is raised when the user
        /// successfully logs an intent to purchase with Google Play.
        /// </summary>
        event OnPurchaseProductDelegate OnPurchaseProduct;

        /// <summary>
        /// Occurs after a successful products restored transactions with Google Play.
        /// </summary>
        event OnRestoreProductsDelegate OnRestoreProducts;

        /// <summary>
        /// Occurs when there is an error querying inventory from Google Play Services.
        /// </summary>
        event OnQueryInventoryErrorDelegate OnQueryInventoryError;

        /// <summary>
        /// Occurs when the user attempts to buy a product and there is an error.
        /// </summary>
        event OnPurchaseProductErrorDelegate OnPurchaseProductError;

        /// <summary>
        /// Occurs when the user attempts to restore products and there is an error.
        /// </summary>
        event OnRestoreProductsErrorDelegate OnRestoreProductsError;

        /// <summary>
        /// Occurs when on user canceled.
        /// </summary>
        event OnUserCanceledDelegate OnUserCanceled;

        /// <summary>
        /// Occurs when there is an in app billing procesing error.
        /// </summary>
        event OnInAppBillingProcessingErrorDelegate OnInAppBillingProcesingError;

        /// <summary>
        /// Raised when Google Play Services returns an invalid bundle from previously
        /// purchased items
        /// </summary>
        event OnInvalidOwnedItemsBundleReturnedDelegate OnInvalidOwnedItemsBundleReturned;

        /// <summary>
        /// Occurs when a previously purchased product fails to validate.
        /// </summary>
        event OnPurchaseFailedValidationDelegate OnPurchaseFailedValidation;

And finally add these delegates in the ```IFortumaInAppService``` file outside of the interface definition. The delegate definitions are used in the event signatures:

    public delegate void OnQueryInventoryDelegate();

    public delegate void OnPurchaseProductDelegate();

    public delegate void OnRestoreProductsDelegate();

    public delegate void OnQueryInventoryErrorDelegate(int responseCode, IDictionary<string, object> skuDetails);

    public delegate void OnPurchaseProductErrorDelegate(int responseCode, string sku);

    public delegate void OnRestoreProductsErrorDelegate(int responseCode, IDictionary<string, object> skuDetails);

    public delegate void OnUserCanceledDelegate();

    public delegate void OnInAppBillingProcessingErrorDelegate(string message);

    public delegate void OnInvalidOwnedItemsBundleReturnedDelegate(IDictionary<string, object> ownedItems);

    public delegate void OnPurchaseFailedValidationDelegate(InAppPurchase purchase, string purchaseData, string purchaseSignature);

### The Implementation
I won't be showing code in this section - only pointing you to the necessary files in the GitHub repository to add to your implementation. Actual code will be exposed in the Code Walk-through below.

1. Add a Services folder to your iOS and Android platform project.
2. Add the following file from the GitHub repository ([https://github.com/simsip-admin/InApp](https://github.com/simsip-admin/InApp "InApp")) into each Services folder.
   * InAppService.cs

### Android Configuration
We'll see how all of this hooks-up in the Code Walk-through section below.

### The Sample App
I have kept the UI and MVVM architecture as simple as possible so that the focus can be on the transaction flows for IAP.

### Code Walk-through

#### Initializing

#### Querying Inventory

#### Making a Purchase

#### Restoring a Purchase

### Testing

### Publishing

   








