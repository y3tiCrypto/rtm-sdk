namespace RaptoreumSdk

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography

type TxInput() =
    member val Txid = "" with get, set
    member val Vout = 0u with get, set
    member val ScriptPubKey = Array.empty<byte> with get, set
    member val Amount = 0UL with get, set
    member val ScriptSig = Array.empty<byte> with get, set

type TxOutput() =
    member val Value = 0UL with get, set
    member val Script = Array.empty<byte> with get, set

type UTXO() =
    member val txid = "" with get, set
    member val vout = 0u with get, set
    member val amount = 0.0 with get, set
    member val scriptPubKey = "" with get, set

type RaptoreumTransactionBuilder() =
    let inputs = new List<TxInput>()
    let outputs = new List<TxOutput>()
    
    member val Locktime = 0u with get, set
    member val Version = 1u with get, set

    member this.Inputs = inputs
    member this.Outputs = outputs

    static member private HexDecode(hex: string) : byte[] =
        let bytes = Array.zeroCreate (hex.Length / 2)
        for i in 0 .. bytes.Length - 1 do
            bytes.[i] <- Convert.ToByte(hex.Substring(i * 2, 2), 16)
        bytes

    static member private EncodeVarInt(n: int64) : byte[] =
        if n < 0xfdL then
            [| byte n |]
        elif n <= 0xffffL then
            let buf = Array.zeroCreate 3
            buf.[0] <- 0xfduy
            let temp = BitConverter.GetBytes(uint16 n)
            if not BitConverter.IsLittleEndian then Array.Reverse(temp)
            Array.Copy(temp, 0, buf, 1, 2)
            buf
        elif n <= 0xffffffffL then
            let buf = Array.zeroCreate 5
            buf.[0] <- 0xfeuy
            let temp = BitConverter.GetBytes(uint32 n)
            if not BitConverter.IsLittleEndian then Array.Reverse(temp)
            Array.Copy(temp, 0, buf, 1, 4)
            buf
        else
            let buf = Array.zeroCreate 9
            buf.[0] <- 0xffuy
            let temp = BitConverter.GetBytes(uint64 n)
            if not BitConverter.IsLittleEndian then Array.Reverse(temp)
            Array.Copy(temp, 0, buf, 1, 8)
            buf

    static member private AddressToHash160(address: string) : byte[] =
        let B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"
        let mutable n = System.Numerics.BigInteger.Zero
        for i in 0 .. address.Length - 1 do
            let idx = B58.IndexOf(address.[i])
            if idx = -1 then raise (new FormatException("Invalid Base58 char"))
            n <- n * new System.Numerics.BigInteger(58) + new System.Numerics.BigInteger(idx)
            
        let mutable bytes = n.ToByteArray()
        Array.Reverse(bytes)
        if bytes.Length > 25 && bytes.[0] = 0uy then
            let temp = Array.zeroCreate (bytes.Length - 1)
            Array.Copy(bytes, 1, temp, 0, temp.Length)
            bytes <- temp
        if bytes.Length < 25 then
            let padded = Array.zeroCreate 25
            Array.Copy(bytes, 0, padded, 25 - bytes.Length, bytes.Length)
            bytes <- padded
            
        let payload = Array.zeroCreate 21
        let checksum = Array.zeroCreate 4
        Array.Copy(bytes, 0, payload, 0, 21)
        Array.Copy(bytes, 21, checksum, 0, 4)

        using (SHA256.Create()) (fun sha ->
            let h1 = sha.ComputeHash(payload)
            let h2 = sha.ComputeHash(h1)
            for i in 0 .. 3 do
                if h2.[i] <> checksum.[i] then raise (new FormatException("Invalid address checksum"))
        )

        let hash160 = Array.zeroCreate 20
        Array.Copy(payload, 1, hash160, 0, 20)
        hash160

    member this.AddInput(txid: string, vout: uint32, scriptPubKey: string, amountRtm: double) =
        let inVal = new TxInput()
        inVal.Txid <- txid
        inVal.Vout <- vout
        inVal.ScriptPubKey <- RaptoreumTransactionBuilder.HexDecode(scriptPubKey)
        inVal.Amount <- uint64 (Math.Round(amountRtm * 100000000.0))
        inputs.Add(inVal)

    member this.AddOutput(address: string, amountRtm: double) =
        let hash160 = RaptoreumTransactionBuilder.AddressToHash160(address)
        let script = Array.zeroCreate 25
        script.[0] <- 0x76uy
        script.[1] <- 0xa9uy
        script.[2] <- 0x14uy
        Array.Copy(hash160, 0, script, 3, 20)
        script.[23] <- 0x88uy
        script.[24] <- 0xacuy
        
        let outVal = new TxOutput()
        outVal.Value <- uint64 (Math.Round(amountRtm * 100000000.0))
        outVal.Script <- script
        outputs.Add(outVal)

    member this.Serialize() : byte[] =
        using (new MemoryStream()) (fun ms ->
            let verBytes = BitConverter.GetBytes(this.Version)
            if not BitConverter.IsLittleEndian then Array.Reverse(verBytes)
            ms.Write(verBytes, 0, 4)

            let inCount = RaptoreumTransactionBuilder.EncodeVarInt(int64 inputs.Count)
            ms.Write(inCount, 0, inCount.Length)

            for input in inputs do
                let txidBytes = RaptoreumTransactionBuilder.HexDecode(input.Txid)
                Array.Reverse(txidBytes)
                ms.Write(txidBytes, 0, txidBytes.Length)

                let voutBytes = BitConverter.GetBytes(input.Vout)
                if not BitConverter.IsLittleEndian then Array.Reverse(voutBytes)
                ms.Write(voutBytes, 0, 4)

                let scriptSigLen = RaptoreumTransactionBuilder.EncodeVarInt(int64 input.ScriptSig.Length)
                ms.Write(scriptSigLen, 0, scriptSigLen.Length)
                ms.Write(input.ScriptSig, 0, input.ScriptSig.Length)

                let seqBytes = BitConverter.GetBytes(0xffffffffu)
                ms.Write(seqBytes, 0, 4)

            let outCount = RaptoreumTransactionBuilder.EncodeVarInt(int64 outputs.Count)
            ms.Write(outCount, 0, outCount.Length)

            for output in outputs do
                let valBytes = BitConverter.GetBytes(output.Value)
                if not BitConverter.IsLittleEndian then Array.Reverse(valBytes)
                ms.Write(valBytes, 0, 8)

                let scriptLen = RaptoreumTransactionBuilder.EncodeVarInt(int64 output.Script.Length)
                ms.Write(scriptLen, 0, scriptLen.Length)
                ms.Write(output.Script, 0, output.Script.Length)

            let ltBytes = BitConverter.GetBytes(this.Locktime)
            if not BitConverter.IsLittleEndian then Array.Reverse(ltBytes)
            ms.Write(ltBytes, 0, 4)

            ms.ToArray()
        )

    member this.Sign(privateKeyBytes: byte[]) =
        let mutable ecParams = new ECParameters()
        ecParams.Curve <- ECCurve.CreateFromFriendlyName("secp256k1")
        ecParams.D <- privateKeyBytes
        using (ECDsa.Create(ecParams)) (fun ecdsa ->
            let parameters = ecdsa.ExportParameters(false)
            let qX = parameters.Q.X
            let qY = parameters.Q.Y
            if qX = null || qY = null then raise (new InvalidOperationException("Invalid key points"))
            
            let prefix = if qY.[qY.Length - 1] % 2uy = 0uy then 0x02uy else 0x03uy
            let pubBytes = Array.zeroCreate 33
            pubBytes.[0] <- prefix
            Array.Copy(qX, 0, pubBytes, 1, 32)

            for i in 0 .. inputs.Count - 1 do
                let originalScriptSigs = Array.zeroCreate inputs.Count
                for j in 0 .. inputs.Count - 1 do
                    originalScriptSigs.[j] <- inputs.[j].ScriptSig

                for j in 0 .. inputs.Count - 1 do
                    if j = i then
                        inputs.[j].ScriptSig <- inputs.[j].ScriptPubKey
                    else
                        inputs.[j].ScriptSig <- Array.empty<byte>

                let serialized = this.Serialize()
                let preimage = Array.zeroCreate (serialized.Length + 4)
                Array.Copy(serialized, preimage, serialized.Length)
                preimage.[serialized.Length] <- 0x01uy // SIGHASH_ALL

                let sigVal = RaptoreumWallet.SignMessage(privateKeyBytes, preimage)
                let sigWithHash = Array.zeroCreate (sigVal.Length + 1)
                Array.Copy(sigVal, sigWithHash, sigVal.Length)
                sigWithHash.[sigVal.Length] <- 0x01uy // SIGHASH_ALL

                let scriptSig = Array.zeroCreate (1 + sigWithHash.Length + 1 + pubBytes.Length)
                scriptSig.[0] <- byte sigWithHash.Length
                Array.Copy(sigWithHash, 0, scriptSig, 1, sigWithHash.Length)
                scriptSig.[1 + sigWithHash.Length] <- byte pubBytes.Length
                Array.Copy(pubBytes, 0, scriptSig, 1 + sigWithHash.Length + 1, pubBytes.Length)

                for j in 0 .. inputs.Count - 1 do
                    inputs.[j].ScriptSig <- originalScriptSigs.[j]
                inputs.[i].ScriptSig <- scriptSig
        )

    static member SelectInputs(utxos: List<UTXO>, targetAmountRtm: double, feeRateSatByte: uint32) : List<UTXO> * double =
        let targetSat = uint64 (Math.Round(targetAmountRtm * 100000000.0))
        let mutable accumulated = 0UL
        let selected = new List<UTXO>()
        let numOutputs = 2u

        let mutable finished = false
        for utxo in utxos do
            if not finished then
                selected.Add(utxo)
                accumulated <- accumulated + uint64 (Math.Round(utxo.amount * 100000000.0))
                
                let size = 148u * uint32 selected.Count + 34u * numOutputs + 10u
                let fee = uint64 size * uint64 feeRateSatByte
                
                if accumulated >= targetSat + fee then
                    finished <- true
                    
        if accumulated < targetSat then raise (new InvalidOperationException("Insufficient funds"))
        
        let size = 148u * uint32 selected.Count + 34u * numOutputs + 10u
        let finalFee = uint64 size * uint64 feeRateSatByte
        (selected, double finalFee)
