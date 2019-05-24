﻿using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed class GasToken : Nep5Token<Nep5AccountState>
    {
        public override string ServiceName => "Neo.Native.Tokens.GAS";
        public override string Name => "GAS";
        public override string Symbol => "gas";
        public override byte Decimals => 8;

        private const byte Prefix_SystemFeeAmount = 15;

        internal GasToken() : base()
        {
            var list = new List<ContractMethodDescriptor>(Manifest.Abi.Methods)
            {
                new ContractMethodDescriptor()
                {
                    Name = "getSysFeeAmount",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                             Name = "index",
                             Type = ContractParameterType.Integer
                        }
                    },
                    ReturnType = ContractParameterType.Integer
                }
            };

            Manifest.Abi.Methods = list.ToArray();
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            if (operation == "getSysFeeAmount")
                return GetSysFeeAmount(engine.Snapshot, (uint)args[0].GetBigInteger());
            else
                return base.Main(engine, operation, args);
        }

        protected override bool OnPersist(ApplicationEngine engine)
        {
            if (!base.OnPersist(engine)) return false;
            foreach (Transaction tx in engine.Snapshot.PersistingBlock.Transactions)
                Burn(engine, tx.Sender, tx.Gas + tx.NetworkFee);
            ECPoint[] validators = NEO.GetNextBlockValidators(engine.Snapshot);
            UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.Snapshot.PersistingBlock.ConsensusData.PrimaryIndex]).ToScriptHash();
            Mint(engine, primary, engine.Snapshot.PersistingBlock.Transactions.Sum(p => p.NetworkFee));
            BigInteger sys_fee = GetSysFeeAmount(engine.Snapshot, engine.Snapshot.PersistingBlock.Index - 1) + engine.Snapshot.PersistingBlock.Transactions.Sum(p => p.Gas);
            StorageKey key = CreateStorageKey(Prefix_SystemFeeAmount, BitConverter.GetBytes(engine.Snapshot.PersistingBlock.Index));
            engine.Snapshot.Storages.Add(key, new StorageItem
            {
                Value = sys_fee.ToByteArray(),
                IsConstant = true
            });
            return true;
        }

        public BigInteger GetSysFeeAmount(Snapshot snapshot, uint index)
        {
            if (index == 0) return Blockchain.GenesisBlock.Transactions.Sum(p => p.Gas);
            StorageKey key = CreateStorageKey(Prefix_SystemFeeAmount, BitConverter.GetBytes(index));
            StorageItem storage = snapshot.Storages.TryGet(key);
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }
    }
}