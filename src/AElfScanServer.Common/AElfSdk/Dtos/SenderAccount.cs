using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;

namespace AElfScanServer.Common.AElfSdk.Dtos;

public class SenderAccount
{
    private readonly ECKeyPair _keyPair;
    public AElf.Types.Address Address { get; set; }

    public SenderAccount(string privateKey)
    {
        _keyPair = CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey));
        Address = AElf.Types.Address.FromPublicKey(_keyPair.PublicKey);
    }

    public ByteString GetSignatureWith(byte[] txData)
    {
        var signature = CryptoHelper.SignWithPrivateKey(_keyPair.PrivateKey, txData);
        return ByteString.CopyFrom(signature);
    }
}