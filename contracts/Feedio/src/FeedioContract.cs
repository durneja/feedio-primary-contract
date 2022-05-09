using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace Feedio
{
    [DisplayName("Feedio.FeedioContract")]
    [ManifestExtra("Author", "durneja")]
    [ManifestExtra("Email", "kinshuk.kar@gmail.com")]
    [ManifestExtra("Description", "Primary contract for Feedio solution with aggregator interfaces and periodic data updates")]
    [ContractPermission("0x8b6a955ef8026cecaf9393f1734bffe508ce42be", "*")]
    [ContractTrust("0x8b6a955ef8026cecaf9393f1734bffe508ce42be")]
    public class FeedioContract : SmartContract
    {

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;
        private static bool ValidateAddress(UInt160 address) => address.IsValid && !address.IsZero;

        protected const byte Prefix_Config = 0x01;
        protected const string Prefix_Config_Owner = "o";
        protected const string Prefix_Config_Updater = "u";
        protected const string Prefix_Config_Update_Timestamp = "t";

        public delegate void OnPriceUpdatedDelegate();
        [DisplayName("PricesSubscribed")]
        public static event OnPriceUpdatedDelegate OnPriceUpdated;


        private static Boolean VerifyOwner()
        {
            StorageMap configData = new StorageMap(Storage.CurrentContext, Prefix_Config);
            UInt160 owner = (UInt160) configData.Get(Prefix_Config_Owner);
            if (Runtime.CheckWitness(owner)) {return true;}
            return false;
        }

        private static Boolean VerifyUpdater()
        {
            StorageMap configData = new StorageMap(Storage.CurrentContext, Prefix_Config);
            UInt160 updater = (UInt160) configData.Get(Prefix_Config_Updater);
            if (Runtime.CheckWitness(updater)) {return true;}
            return false;
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                initialize();
            }
        }

        private static void initialize() 
        {
            StorageMap configData = new StorageMap(Storage.CurrentContext, Prefix_Config);

            configData.Put(Prefix_Config_Owner, (UInt160) ToScripthash("Nc2JPKy62qCWWWSB6Ud6KL275u8yhWGTj5"));
            configData.Put(Prefix_Config_Updater, (UInt160) ToScripthash("NRurDcircwHDFb2aBuiP2QNPHUqa8evAja"));
            configData.Put((ByteString)"D*" + (ByteString)"BTC", (BigInteger) 4); //9
            configData.Put((ByteString)"D*" + (ByteString)"ETH", (BigInteger) 4); //8
            configData.Put((ByteString)"D*" + (ByteString)"NEO", (BigInteger) 6); //8
            configData.Put((ByteString)"D*" + (ByteString)"GAS", (BigInteger) 6); //9
            configData.Put((ByteString)"D*" + (ByteString)"BNB", (BigInteger) 6); //9
            configData.Put((ByteString)"D*" + (ByteString)"MATIC", (BigInteger) 6); //9

        }
        public static void UpdateTokenPrice(List<ByteString> tokens, List<BigInteger> prices) {

            if (!VerifyOwner() && !VerifyUpdater()) { throw new Exception("Not authorized for executing this method");}

            int i = 0;
            foreach (var token in tokens)
            {
                int j = 0;
                foreach (var price in prices)
                {
                    if (i == j) {
                        Storage.Put(Storage.CurrentContext, (ByteString)"V1" + (ByteString)token, (BigInteger) price);
                        break;
                    }
                    j = j + 1;
                }
                i = i + 1;
            }

            StorageMap configData = new StorageMap(Storage.CurrentContext, Prefix_Config);
            configData.Put(Prefix_Config_Update_Timestamp, (BigInteger)Runtime.Time);

            OnPriceUpdated();
            return;
        }

        public static BigInteger GetLastUpdatedTime() {
            StorageMap configData = new StorageMap(Storage.CurrentContext, Prefix_Config);
            BigInteger lastUpdatedTime = (BigInteger)configData.Get(Prefix_Config_Update_Timestamp);

            return lastUpdatedTime;
        }

        public static ByteString GetLatestTokenPrices() {

            bool hasValidAccessSender = (bool)Contract.Call(ToScripthash("NdFybtEngSHyfRbUCw1vCe9HZbBtpaX741"), "accessPresentAndValid", CallFlags.ReadOnly, Runtime.CallingScriptHash);
            bool hasValidAccessCaller = (bool)Contract.Call(ToScripthash("NdFybtEngSHyfRbUCw1vCe9HZbBtpaX741"), "accessPresentAndValid", CallFlags.ReadOnly, Tx.Sender);

            if (!hasValidAccessSender && !hasValidAccessCaller) throw new Exception("Account does not have required access to retrieve prices");

            var iterator = Storage.Find(Storage.CurrentContext, (ByteString) "V1", FindOptions.KeysOnly | FindOptions.RemovePrefix);
            List<TokenPriceResponse> prices = new List<TokenPriceResponse>();
            
            while (iterator.Next()) {
                
                BigInteger assetPrice = (BigInteger) Storage.Get(Storage.CurrentContext, (ByteString)"V1" + (ByteString)iterator.Value);
                StorageMap configData = new StorageMap(Storage.CurrentContext, Prefix_Config);

                TokenPriceResponse priceResponse = new TokenPriceResponse((ByteString) iterator.Value, assetPrice, (BigInteger) configData.Get((ByteString)"D*" + (ByteString)iterator.Value));
                prices.Add(priceResponse);
            }

            return StdLib.JsonSerialize(prices);
        }

        public static ByteString GetLatestTokenPrice(ByteString asset) {

            bool hasValidAccessSender = (bool)Contract.Call(ToScripthash("NdFybtEngSHyfRbUCw1vCe9HZbBtpaX741"), "accessPresentAndValid", CallFlags.ReadOnly, Runtime.CallingScriptHash);
            bool hasValidAccessCaller = (bool)Contract.Call(ToScripthash("NdFybtEngSHyfRbUCw1vCe9HZbBtpaX741"), "accessPresentAndValid", CallFlags.ReadOnly, Tx.Sender);

            if (!hasValidAccessSender && !hasValidAccessCaller) throw new Exception("Account does not have required access to retrieve prices");

            BigInteger assetPrice = (BigInteger) Storage.Get(Storage.CurrentContext, (ByteString)"V1" + asset);
            StorageMap configData = new StorageMap(Storage.CurrentContext, Prefix_Config);

            TokenPriceResponse response = new TokenPriceResponse(asset, assetPrice, (BigInteger) configData.Get((ByteString)"D*" + asset));
            return StdLib.JsonSerialize(response);
        }

        public static void UpdateContract(ByteString nefFile, string manifest)
        {
            if (!VerifyOwner()) { throw new Exception("Not authorized for executing this method");}
            ContractManagement.Update(nefFile, manifest, null);
        }

        public static void Destroy()
        {
            if (!VerifyOwner()) { throw new Exception("Not authorized for executing this method");}
            ContractManagement.Destroy();
        }

        private static UInt160 ToScripthash(String address) {
            if ((address.ToByteArray())[0] == 0x4e)
            {
                var decoded = (byte[]) StdLib.Base58CheckDecode(address);
                var Scripthash = (UInt160)decoded.Last(20);
                return (Scripthash);
            }
            return null;
        }

    }

    public class TokenPriceResponse {
        public string name; //Token Name
        public BigInteger value; //Value of the token
        public BigInteger decimals; //Decimals

        public TokenPriceResponse(string tokenName, BigInteger tokenValue, BigInteger decimals) {
            this.name = tokenName;
            this.value = tokenValue;
            this.decimals = decimals;
        }
    }
}
