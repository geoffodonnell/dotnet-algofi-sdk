using Algofi.V1.Model;
using Algorand;
using Algorand.Common;
using Algorand.V2.Algod.Model;
using Algorand.V2.Indexer.Model;
using Org.BouncyCastle.Utilities;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Transaction = Algorand.Transaction;

namespace Algofi.V1.Transactions {

	public static class PrependTransactions {

		public const int PaddingTransactionCount = 9;
		public readonly static Dictionary<int, string> PaddingWords = new Dictionary<int, string> {
			{ 1, "one" }, {2, "two"}, {3, "three"},
			{ 4, "four" }, { 5, "five" }, { 6, "six"},
			{ 7, "seven"}, { 8, "eight" }, {9, "nine" },
			{ 10, "ten" }
		};

		private readonly static Random mRandom = new Random(DateTime.Now.Millisecond);

		public static TransactionGroup GetInitTransactions(
			AlgofiOperation operation,
			Address sender,
			TransactionParametersResponse txParams,
			ulong managerAppId,
			ulong[] supportedMarketAppIds,
			ulong[] supportedOracleAppIds,
			Address storageAccount) {

			var txn0 = Utils.GetApplicationCallTransaction(
				sender,
				managerAppId,
				txParams,
				new List<byte[]> {
					Strings.ToUtf8ByteArray(ManagerStrings.FetchMarketVariables)
				});

			txn0.onCompletion = OnCompletion.Noop;
			txn0.foreignApps = new List<ulong>(supportedMarketAppIds);
			txn0.note = IntToBytes(mRandom.Next(1000000));

			var txn1 = Utils.GetApplicationCallTransaction(
				sender,
				managerAppId,
				txParams,
				new List<byte[]> {
					Strings.ToUtf8ByteArray(ManagerStrings.UpdatePrices)
				});

			txn1.onCompletion = OnCompletion.Noop;
			txn1.foreignApps = new List<ulong>(supportedOracleAppIds);
			txn1.fee = UpdatePricesFee(operation);

			var txn2 = Utils.GetApplicationCallTransaction(
				sender,
				managerAppId,
				txParams,
				new List<byte[]> {
					Strings.ToUtf8ByteArray(ManagerStrings.UpdateProtocolData)
				});

			txn2.onCompletion = OnCompletion.Noop;
			txn2.foreignApps = new List<ulong>(supportedMarketAppIds);
			txn2.accounts = new List<Address>(new[] { storageAccount });

			var txns = new List<Transaction>() {
				txn0, txn1, txn2
			};

			for (var i = 0; i < PaddingTransactionCount; i++) {
				var txn = Utils.GetApplicationCallTransaction(
					sender,
					managerAppId,
					txParams,
					new List<byte[]> {
						Strings.ToUtf8ByteArray($"dummy_{PaddingWords[i + 1]}")
					});

				txn.onCompletion = OnCompletion.Noop;
				txn.foreignApps = new List<ulong>(supportedMarketAppIds);

				txns.Add(txn);
			}

			return new TransactionGroup(txns);
		}

		private static byte[] IntToBytes(int value) {

			var result = new byte[8];

			BinaryPrimitives.WriteInt64BigEndian(result, value);

			return result;
		}

		private static ulong UpdatePricesFee(AlgofiOperation operation) {

			switch (operation) {
				case AlgofiOperation.Mint:
				case AlgofiOperation.Burn:
				case AlgofiOperation.RemoveCollateral:
				case AlgofiOperation.RemoveCollateralUnderlying:
				case AlgofiOperation.Borrow:
				case AlgofiOperation.RepayBorrow:
				case AlgofiOperation.Liquidate:
				case AlgofiOperation.ClaimRewards:
					return 2000;
				default: return 1000;
			}

		}

	}

}
