use secp256k1::{Secp256k1, SecretKey, PublicKey};
use sha2::{Sha256, Digest};
use std::error::Error;
use crate::wallet::RaptoreumWallet;

#[derive(Clone)]
pub struct TxInput {
    pub txid: String,
    pub vout: u32,
    pub script_pub_key: Vec<u8>,
    pub amount: u64,
    pub script_sig: Vec<u8>,
}

#[derive(Clone)]
pub struct TxOutput {
    pub value: u64,
    pub script: Vec<u8>,
}

#[derive(Clone)]
pub struct UTXO {
    pub txid: String,
    pub vout: u32,
    pub amount: f64,
    pub script_pub_key: String,
}

pub struct RaptoreumTransactionBuilder {
    pub inputs: Vec<TxInput>,
    pub outputs: Vec<TxOutput>,
    pub locktime: u32,
    pub version: u32,
}

impl RaptoreumTransactionBuilder {
    pub fn new() -> Self {
        RaptoreumTransactionBuilder {
            inputs: Vec::new(),
            outputs: Vec::new(),
            locktime: 0,
            version: 1,
        }
    }

    pub fn add_input(&mut self, txid: &str, vout: u32, script_pub_key: &str, amount_rtm: f64) -> Result<(), Box<dyn Error>> {
        let spk = hex::decode(script_pub_key)?;
        self.inputs.push(TxInput {
            txid: txid.to_string(),
            vout,
            script_pub_key: spk,
            amount: (amount_rtm * 100000000.0).round() as u64,
            script_sig: Vec::new(),
        });
        Ok(())
    }

    pub fn add_output(&mut self, address: &str, amount_rtm: f64) -> Result<(), Box<dyn Error>> {
        let hash160 = address_to_hash160(address)?;
        let mut script = vec![0x76, 0xa9, 0x14];
        script.extend_from_slice(&hash160);
        script.extend_from_slice(&[0x88, 0xac]);

        self.outputs.push(TxOutput {
            value: (amount_rtm * 100000000.0).round() as u64,
            script,
        });
        Ok(())
    }

    pub fn serialize(&self) -> Result<Vec<u8>, Box<dyn Error>> {
        let mut res = Vec::new();
        res.extend_from_slice(&self.version.to_le_bytes());
        
        res.extend_from_slice(&encode_varint(self.inputs.len()));
        for input in &self.inputs {
            let mut txid_bytes = hex::decode(&input.txid)?;
            txid_bytes.reverse();
            res.extend_from_slice(&txid_bytes);
            res.extend_from_slice(&input.vout.to_le_bytes());
            
            res.extend_from_slice(&encode_varint(input.script_sig.len()));
            res.extend_from_slice(&input.script_sig);
            
            res.extend_from_slice(&0xffffffffu32.to_le_bytes());
        }

        res.extend_from_slice(&encode_varint(self.outputs.len()));
        for output in &self.outputs {
            res.extend_from_slice(&output.value.to_le_bytes());
            res.extend_from_slice(&encode_varint(output.script.len()));
            res.extend_from_slice(&output.script);
        }

        res.extend_from_slice(&self.locktime.to_le_bytes());
        Ok(res)
    }

    pub fn sign(&mut self, private_key_bytes: &[u8; 32]) -> Result<(), Box<dyn Error>> {
        let secp = Secp256k1::new();
        let secret_key = SecretKey::from_slice(private_key_bytes)?;
        let public_key = PublicKey::from_secret_key(&secp, &secret_key);
        let pub_bytes = public_key.serialize(); // compressed 33 bytes

        for i in 0..self.inputs.len() {
            let original_sigs: Vec<Vec<u8>> = self.inputs.iter().map(|in_val| in_val.script_sig.clone()).collect();

            for j in 0..self.inputs.len() {
                if j == i {
                    self.inputs[j].script_sig = self.inputs[j].script_pub_key.clone();
                } else {
                    self.inputs[j].script_sig = Vec::new();
                }
            }

            let mut preimage = self.serialize()?;
            preimage.extend_from_slice(&1u32.to_le_bytes()); // SIGHASH_ALL

            let sig = RaptoreumWallet::sign_message(private_key_bytes, &preimage)?;
            let mut sig_with_hash = sig;
            sig_with_hash.push(0x01); // SIGHASH_ALL

            let mut script_sig = vec![sig_with_hash.len() as u8];
            script_sig.extend_from_slice(&sig_with_hash);
            script_sig.push(pub_bytes.len() as u8);
            script_sig.extend_from_slice(&pub_bytes);

            for j in 0..self.inputs.len() {
                self.inputs[j].script_sig = original_sigs[j].clone();
            }
            self.inputs[i].script_sig = script_sig;
        }
        Ok(())
    }
}

pub fn select_inputs(utxos: &[UTXO], target_amount_rtm: f64, fee_rate_sat_byte: u64) -> Result<(Vec<UTXO>, u64), Box<dyn Error>> {
    let target_sat = (target_amount_rtm * 100000000.0).round() as u64;
    let mut accumulated = 0u64;
    let mut selected = Vec::new();
    let num_outputs = 2u64;

    for utxo in utxos {
        selected.push(utxo.clone());
        accumulated += (utxo.amount * 100000000.0).round() as u64;

        let size = 148 * selected.len() as u64 + 34 * num_outputs + 10;
        let fee = size * fee_rate_sat_byte;

        if accumulated >= target_sat + fee {
            return Ok((selected, fee));
        }
    }
    Err("Insufficient funds".into())
}

fn encode_varint(val: usize) -> Vec<u8> {
    if val < 0xfd {
        vec![val as u8]
    } else if val <= 0xffff {
        let mut res = vec![0xfd];
        res.extend_from_slice(&(val as u16).to_le_bytes());
        res
    } else if val <= 0xffffffff {
        let mut res = vec![0xfe];
        res.extend_from_slice(&(val as u32).to_le_bytes());
        res
    } else {
        let mut res = vec![0xff];
        res.extend_from_slice(&(val as u64).to_le_bytes());
        res
    }
}

fn address_to_hash160(address: &str) -> Result<Vec<u8>, Box<dyn Error>> {
    let bytes = bs58::decode(address).into_vec()?;
    if bytes.len() < 25 {
        return Err("Invalid address length".into());
    }
    let payload = &bytes[0..21];
    let checksum = &bytes[21..25];

    let mut hasher = Sha256::new();
    hasher.update(payload);
    let h1 = hasher.finalize();

    let mut hasher2 = Sha256::new();
    hasher2.update(&h1);
    let h2 = hasher2.finalize();

    if &h2[0..4] != checksum {
        return Err("Invalid address checksum".into());
    }
    Ok(payload[1..].to_vec())
}
