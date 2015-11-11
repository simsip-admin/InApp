---
layout: home
title: Bill McRoberts
home: true
---

## Xamarin.Forms Dependency Service for In-App Purchases

The challenge with cross-platform in-app purchasing (IAP) is that the transaction flows are so different across iOS and Android. In the game [Linerunner 3D](http://linerunner3d.com "Linerunner 3D"), I wanted a consistent interface to code against for IAP. While I was able to accomplish this, the code-base was shared code not PCL so I ended up with a lot of `#defines`. With Xamarin.Forms and the DependencyService we have a cleaner way of exposing platform-specific functionality exposed via a consistent API. This sample exposes platform specific IAP functionality via a single interface that can be consumed in a Xamarin.Forms PCL.  

<img src="images/screenshot-ios-inapp.PNG" width="40%">  <img src="images/screenshot-android-inapp.png" width="40%">


### Setup
1. In Visual Studio, start with a new `Blank App (Xamarin.Forms.Portable)`, and remove the Windows Phone platform project.

#### iOS

>  See the Xamarin [In-App Purchase Basics and Configuration](http://developer.xamarin.com/guides/ios/application_fundamentals/in-app_purchasing/part_1_-_in-app_purchase_basics_and_configuration/ "In-App Purchase Basics and Configuration") page for details on the following steps.

1. Submit your banking and taxation information to Apple for your Apple Developer Account.
1. Ensure your app has a valid App ID (not a wildcard with an asterisk * in it) and has In App Purchasing enabled in the iOS Provisioning Portal.
1. Add products to your application. in iTunes Connect Application Management


#### Android
2. In the Android platform project add the [Xamarin.InAppBilling](https://components.xamarin.com/view/xamarin.inappbilling "Xamarin.InAppBilling") component.
3. In the `AndroidManifest.xml` file, add the `<uses-permission android:name="com.android.vending.BILLING" />` line between the `<manifest>...</manifest>` tags.
3. Install the Google Play Billing Library. On the [Xamarin.InAppBilling Getting Started](https://components.xamarin.com/gettingstarted/xamarin.inappbilling "Xamarin.InAppBilling Getting Started") page, see the `Installing the Google Play Billing Library` section for detailed steps.
4. In the Google Play Developer Console, if you have not already done so, create:
  * An app that will host our IAP transactions
  * A digital good to sell via IAP
  * A linked Google Wallet Merchant Account

> On the [Xamarin.InAppBilling Getting Started](https://components.xamarin.com/gettingstarted/xamarin.inappbilling "Xamarin.InAppBilling Getting Started") page, see the `Preparing Your App for In-App Billing` section for details on the above steps.
 

### The Interface
The interface exposes an API and set of events to consume IAP.

> But wait you say, can't we do this with a Task based API instead of hooking up events? Hang in there, that's exactly where I want to evolve this sample in the future.

In your portable project create a Services folder and in the Service folder create an interface file ```IInAppService```.

Add the following API to the interface:

        
        /// <summary>
        /// A product id we can use for testing purposes.
        /// </summary>
        string PracticeModeProductId { get; }

        /// <summary>
        /// Initializes the platform specific In-App Purchasing service.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Queries your product inventory asynchronously against your 
        /// platform specific In-App Purchasing service.
        /// </summary>
        void QueryInventory();

        /// <summary>
        /// Attempts to purchase a given product identified by a product 
        /// against your platform specific In-App Purchasing service.
        /// </summary>
        void PurchaseProduct(string productId);

        /// <summary>
        /// Attempts to restore products a user has already purchased.
        /// </summary>
        void RestoreProducts();

        /// <summary>
        /// For testing purposes only on Android.
        /// 
        /// Allows for a convenient hook-up in your administrative ui to
        /// clear out a previous purchase.
        /// </summary>
        void RefundProduct();

Add the following events to the interface:

        /// <summary>
        /// Occurs when a query inventory transactions completes successfully 
        /// against your platform specific In-App Purchasing service.
        /// </summary>
        event OnQueryInventoryDelegate OnQueryInventory;

        /// <summary>
        /// Occurs after a product has been successfully purchased with your 
        /// platform specific In-App Purchasing service.
        /// </summary>
        event OnPurchaseProductDelegate OnPurchaseProduct;

        /// <summary>
        /// Occurs after a successful products restored transaction with your 
        /// platform specific In-App Purchasing service.
        /// </summary>
        event OnRestoreProductsDelegate OnRestoreProducts;

        /// <summary>
        /// Occurs when there is an error querying inventory from your platform 
        /// specific In-App Purchasing service.
        /// </summary>
        event OnQueryInventoryErrorDelegate OnQueryInventoryError;

        /// <summary>
        /// Occurs when the user attempts to buy a product and there is 
        /// an error.
        /// </summary>
        event OnPurchaseProductErrorDelegate OnPurchaseProductError;

        /// <summary>
        /// Occurs when the user attempts to restore products and there is 
        /// an error.
        /// </summary>
        event OnRestoreProductsErrorDelegate OnRestoreProductsError;

        /// <summary>
        /// Occurs when a user cancels.
        /// </summary>
        event OnUserCanceledDelegate OnUserCanceled;

        /// <summary>
        /// Occurs when there is an in app billing procesing error.
        /// </summary>
        event OnInAppBillingProcessingErrorDelegate OnInAppBillingProcesingError;

        /// <summary>
        /// ANDROID ONLY
        ///
        /// Raised when Google Play Services returns an invalid bundle from 
        /// previously purchased items
        /// </summary>
        event OnInvalidOwnedItemsBundleReturnedDelegate 
            OnInvalidOwnedItemsBundleReturned;

        /// <summary>
        /// ANDROID ONLY
        ///
        /// Occurs when a previously purchased product fails to validate.
        /// </summary>
        event OnPurchaseFailedValidationDelegate OnPurchaseFailedValidation;

And finally add these delegates in the ```IInAppService``` file outside of the interface definition. The delegate definitions are used in the event signatures:

    public delegate void OnQueryInventoryDelegate();

    public delegate void OnPurchaseProductDelegate();

    public delegate void OnRestoreProductsDelegate();

    public delegate void OnQueryInventoryErrorDelegate(int responseCode, 
        IDictionary<string, object> skuDetails);

    public delegate void OnPurchaseProductErrorDelegate(int responseCode, 
        string sku);

    public delegate void OnRestoreProductsErrorDelegate(int responseCode, 
       IDictionary<string, object> skuDetails);

    public delegate void OnUserCanceledDelegate();

    public delegate void OnInAppBillingProcessingErrorDelegate(string message);

    public delegate void OnInvalidOwnedItemsBundleReturnedDelegate(
        IDictionary<string, object> ownedItems);

    public delegate void OnPurchaseFailedValidationDelegate(InAppPurchase purchase, 
        string purchaseData, string purchaseSignature);

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

   








