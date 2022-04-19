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

        private static Boolean VerifyOwner()
        {
            StorageMap configMap = new(Storage.CurrentContext, Prefix_Config);
            UInt160 owner = (UInt160) configMap.Get(Prefix_Config_Owner);
            if (Runtime.CheckWitness(owner))
            {
                return true;
            }
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
            StorageMap configMap = new(Storage.CurrentContext, Prefix_Config);
            configMap.Put(Prefix_Config_Owner, (UInt160) Tx.Sender);
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
    }
}
