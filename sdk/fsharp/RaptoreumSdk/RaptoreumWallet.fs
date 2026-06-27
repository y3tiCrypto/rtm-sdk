namespace RaptoreumSdk

open System
open System.Security.Cryptography

module private PureRipemd =
    let r = [|
        0; 1; 2; 3; 4; 5; 6; 7; 8; 9; 10; 11; 12; 13; 14; 15;
        7; 4; 13; 1; 10; 6; 15; 3; 12; 0; 9; 5; 2; 14; 11; 8;
        3; 10; 14; 4; 9; 15; 8; 1; 2; 7; 0; 6; 13; 11; 5; 12;
        1; 9; 11; 10; 0; 8; 12; 4; 13; 3; 7; 15; 14; 5; 6; 2;
        4; 0; 5; 9; 7; 12; 2; 10; 14; 1; 3; 8; 11; 6; 15; 13
    |]

    let rp = [|
        5; 14; 7; 0; 9; 2; 11; 4; 13; 6; 15; 8; 1; 10; 3; 12;
        6; 11; 3; 7; 0; 13; 5; 10; 14; 15; 8; 12; 4; 9; 1; 2;
        15; 5; 1; 3; 7; 14; 6; 9; 11; 8; 12; 2; 10; 0; 4; 13;
        8; 6; 4; 1; 3; 11; 15; 0; 5; 12; 2; 13; 9; 7; 10; 14;
        12; 15; 10; 4; 1; 5; 8; 7; 6; 2; 13; 14; 0; 3; 9; 11
    |]

    let s = [|
        11; 14; 15; 12; 5; 8; 7; 9; 11; 13; 14; 15; 6; 7; 9; 8;
        7; 6; 8; 13; 11; 9; 7; 15; 7; 12; 15; 9; 11; 7; 13; 12;
        11; 13; 6; 7; 14; 9; 13; 15; 14; 8; 13; 6; 5; 12; 7; 5;
        11; 12; 14; 15; 14; 15; 9; 8; 9; 14; 5; 6; 8; 6; 5; 12;
        9; 15; 5; 11; 6; 8; 13; 12; 5; 12; 13; 14; 11; 8; 5; 6
    |]

    let sp = [|
        8; 9; 9; 11; 13; 15; 15; 5; 7; 7; 8; 11; 14; 14; 12; 6;
        9; 13; 15; 7; 12; 8; 9; 11; 7; 7; 12; 7; 6; 15; 13; 11;
        9; 7; 15; 11; 8; 6; 6; 14; 12; 13; 5; 14; 13; 13; 7; 5;
        15; 5; 8; 11; 14; 14; 6; 14; 6; 9; 12; 9; 12; 5; 15; 8;
        8; 5; 12; 9; 12; 5; 14; 6; 8; 13; 6; 5; 15; 13; 11; 11
    |]

    let rol (value: uint32) (count: int) =
        (value <<< count) ||| (value >>> (32 - count))

    let computeRipemd160 (input: byte[]) : byte[] =
        let X = Array.zeroCreate 16
        for i in 0 .. 7 do
            X.[i] <- BitConverter.ToUInt32(input, i * 4)
        X.[8] <- 0x80u
        X.[14] <- 256u

        let mutable h0 = 0x67452301u
        let mutable h1 = 0xefcdab89u
        let mutable h2 = 0x98badcfeu
        let mutable h3 = 0x10325476u
        let mutable h4 = 0xc3d2e1f0u

        let mutable A = h0
        let mutable B = h1
        let mutable C = h2
        let mutable D = h3
        let mutable E = h4

        let mutable Ap = h0
        let mutable Bp = h1
        let mutable Cp = h2
        let mutable Dp = h3
        let mutable Ep = h4

        for j in 0 .. 79 do
            let mutable f = 0u
            let mutable K = 0u
            if j < 16 then
                f <- B ^^^ C ^^^ D
                K <- 0x00000000u
            elif j < 32 then
                f <- (B &&& C) ||| ((~~~B) &&& D)
                K <- 0x5a827999u
            elif j < 48 then
                f <- (B ||| (~~~C)) ^^^ D
                K <- 0x6ed9eba1u
            elif j < 64 then
                f <- (B &&& D) ||| (C &&& (~~~D))
                K <- 0x8f1bbcdcu
            else
                f <- B ^^^ (C ||| (~~~D))
                K <- 0xa6c302ffu

            let T = (rol (A + f + X.[r.[j]] + K) s.[j]) + E
            A <- E; E <- D; D <- rol C 10; C <- B; B <- T

            let mutable fp = 0u
            let mutable Kp = 0u
            if j < 16 then
                fp <- Bp ^^^ (Cp ||| (~~~Dp))
                Kp <- 0x50a28be6u
            elif j < 32 then
                fp <- (Bp &&& Dp) ||| (Cp &&& (~~~Dp))
                Kp <- 0x5c4dd124u
            elif j < 48 then
                fp <- (Bp ||| (~~~Cp)) ^^^ Dp
                Kp <- 0x6d703ef3u
            elif j < 64 then
                fp <- (Bp &&& Cp) ||| ((~~~Bp) &&& Dp)
                Kp <- 0x7a6d76e9u
            else
                fp <- Bp ^^^ Cp ^^^ Dp
                Kp <- 0x00000000u

            let Tp = (rol (Ap + fp + X.[rp.[j]] + Kp) sp.[j]) + Ep
            Ap <- Ep; Ep <- Dp; Dp <- rol Cp 10; Cp <- Bp; Bp <- Tp

        let temp = h1 + C + Dp
        h1 <- h2 + D + Ep
        h2 <- h3 + E + Ap
        h3 <- h4 + A + Bp
        h4 <- h0 + B + Cp
        h0 <- temp

        let result = Array.zeroCreate 20
        Array.Copy(BitConverter.GetBytes(h0), 0, result, 0, 4)
        Array.Copy(BitConverter.GetBytes(h1), 0, result, 4, 4)
        Array.Copy(BitConverter.GetBytes(h2), 0, result, 8, 4)
        Array.Copy(BitConverter.GetBytes(h3), 0, result, 12, 4)
        Array.Copy(BitConverter.GetBytes(h4), 0, result, 16, 4)
        result

type RaptoreumWallet =
    static member private HashSHA256(data: byte[]) : byte[] =
        using (SHA256.Create()) (fun sha ->
            sha.ComputeHash(data)
        )

    static member GeneratePrivateKey() : byte[] =
        using (ECDsa.Create(ECCurve.CreateFromFriendlyName("secp256k1"))) (fun ecdsa ->
            let parameters = ecdsa.ExportParameters(true)
            match parameters.D with
            | null -> raise (new InvalidOperationException("Failed to export D"))
            | d -> d
        )

    static member PrivateKeyToAddress(privateKeyBytes: byte[]) : string =
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
            
            let sha = RaptoreumWallet.HashSHA256(pubBytes)
            let h160 = PureRipemd.computeRipemd160(sha)
                
            let payload = Array.zeroCreate 21
            payload.[0] <- 0x3cuy
            Array.Copy(h160, 0, payload, 1, 20)
            
            let hash1 = RaptoreumWallet.HashSHA256(payload)
            let hash2 = RaptoreumWallet.HashSHA256(hash1)
            let checksum = Array.zeroCreate 4
            Array.Copy(hash2, 0, checksum, 0, 4)
            
            let fullPayload = Array.zeroCreate 25
            Array.Copy(payload, 0, fullPayload, 0, 21)
            Array.Copy(checksum, 0, fullPayload, 21, 4)
            
            RaptoreumWallet.Base58Encode(fullPayload)
        )

    static member SignMessage(privateKeyBytes: byte[], messageBytes: byte[]) : byte[] =
        let mutable ecParams = new ECParameters()
        ecParams.Curve <- ECCurve.CreateFromFriendlyName("secp256k1")
        ecParams.D <- privateKeyBytes
        using (ECDsa.Create(ecParams)) (fun ecdsa ->
            let hash1 = RaptoreumWallet.HashSHA256(messageBytes)
            let hash2 = RaptoreumWallet.HashSHA256(hash1)
            ecdsa.SignHash(hash2)
        )

    static member private Base58Encode(data: byte[]) : string =
        let B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"
        let mutable n = System.Numerics.BigInteger.Zero
        for i in 0 .. data.Length - 1 do
            n <- n * new System.Numerics.BigInteger(256) + new System.Numerics.BigInteger(int data.[i])
            
        let mutable result = ""
        while n > System.Numerics.BigInteger.Zero do
            let mutable rem = System.Numerics.BigInteger.Zero
            n <- System.Numerics.BigInteger.DivRem(n, new System.Numerics.BigInteger(58), &rem)
            result <- string B58.[int rem] + result
            
        let mutable pad = 0
        let mutable finished = false
        for i in 0 .. data.Length - 1 do
            if not finished && data.[i] = 0uy then
                pad <- pad + 1
            else
                finished <- true
                
        new string('1', pad) + result
