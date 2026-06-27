use secp256k1::{Secp256k1, SecretKey, PublicKey, Message};
use ripemd::{Ripemd160, Digest as RDigest};
use sha2::{Sha256, Digest as SDigest};
use rand::rngs::OsRng;

pub struct RaptoreumWallet;

impl RaptoreumWallet {
    pub fn generate_private_key() -> [u8; 32] {
        let secp = Secp256k1::new();
        let (secret_key, _) = secp.generate_keypair(&mut OsRng);
        secret_key.secret_bytes()
    }

    pub fn private_key_to_address(private_key_bytes: &[u8; 32]) -> Result<String, Box<dyn std::error::Error>> {
        let secp = Secp256k1::new();
        let secret_key = SecretKey::from_slice(private_key_bytes)?;
        let public_key = PublicKey::from_secret_key(&secp, &secret_key);
        let pub_bytes = public_key.serialize();

        // Hash160
        let mut sha = Sha256::new();
        sha.update(&pub_bytes);
        let sha_result = sha.finalize();

        let mut rip = Ripemd160::new();
        rip.update(&sha_result);
        let h160 = rip.finalize();

        // Raptoreum prefix is 0x3c (60)
        let mut payload = Vec::with_capacity(21);
        payload.push(0x3c);
        payload.extend_from_slice(&h160);

        let mut hash1 = Sha256::new();
        hash1.update(&payload);
        let res1 = hash1.finalize();

        let mut hash2 = Sha256::new();
        hash2.update(&res1);
        let checksum = &hash2.finalize()[0..4];

        let mut full_payload = payload.clone();
        full_payload.extend_from_slice(checksum);

        Ok(bs58::encode(full_payload).into_string())
    }

    pub fn sign_message(private_key_bytes: &[u8; 32], message_bytes: &[u8]) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
        let secp = Secp256k1::new();
        let secret_key = SecretKey::from_slice(private_key_bytes)?;

        // Double sha256 of message
        let mut hash1 = Sha256::new();
        hash1.update(message_bytes);
        let res1 = hash1.finalize();

        let mut hash2 = Sha256::new();
        hash2.update(&res1);
        let double_hash = hash2.finalize();

        let msg = Message::from_slice(&double_hash)?;
        let sig = secp.sign_ecdsa(&msg, &secret_key);

        Ok(sig.serialize_der().to_vec())
    }
}
