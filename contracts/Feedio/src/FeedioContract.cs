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
    public class FeedioContract : SmartContract
    {

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;
        private static bool ValidateAddress(UInt160 address) => address.IsValid && !address.IsZero;

        protected const byte Prefix_Config = 0x01;
        protected const string Prefix_Config_Owner = "o";
        protected const string Prefix_Config_Updater = "u";

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

            return;
        }

        public static ByteString GetLatestTokenPrice(ByteString asset) {

            BigInteger assetPrice = (BigInteger) Storage.Get(Storage.CurrentContext, (ByteString)"V1" + asset);

            TokenPriceResponse response = new TokenPriceResponse(asset,assetPrice);
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

        public TokenPriceResponse(string tokenName, BigInteger tokenValue) {
            this.name = tokenName;
            this.value = tokenValue;
            this.decimals = 4;
        }
    }

}
