import Foundation

#if canImport(FoundationNetworking)
import FoundationNetworking
#endif

public struct RPCRequest: Codable {
    public let jsonrpc: String
    public let id: String
    public let method: String
    public let params: [AnyCodable]
}

public struct RPCResponse<T: Codable>: Codable {
    public let result: T?
    public let error: RaptoreumRPCError?
    public let id: String
}

public struct RaptoreumRPCError: Codable, Error {
    public let code: Int
    public let message: String
}

public struct AnyCodable: Codable {
    public let value: Any

    public init(_ value: Any) {
        self.value = value
    }

    public init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()
        if let x = try? container.decode(Bool.self) { self.value = x }
        else if let x = try? container.decode(Int.self) { self.value = x }
        else if let x = try? container.decode(Double.self) { self.value = x }
        else if let x = try? container.decode(String.self) { self.value = x }
        else if let x = try? container.decode([String: Double].self) { self.value = x }
        else { throw DecodingError.dataCorruptedError(in: container, debugDescription: "Wrong type") }
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.singleValueContainer()
        if let x = value as? Bool { try container.encode(x) }
        else if let x = value as? Int { try container.encode(x) }
        else if let x = value as? Double { try container.encode(x) }
        else if let x = value as? String { try container.encode(x) }
        else if let x = value as? [String: Double] { try container.encode(x) }
    }
}

public class RaptoreumClient {
    private let url: URL
    private let authHeader: String?

    public init(host: String = "127.0.0.1", port: Int = 8766, user: String = "", pass: String = "", useSsl: Bool = false) {
        let scheme = useSsl ? "https" : "http"
        self.url = URL(string: "\(scheme)://\(host):\(port)/")!

        if !user.isEmpty || !pass.isEmpty {
            let credentials = "\(user):\(pass)"
            self.authHeader = "Basic " + Data(credentials.utf8).base64EncodedString()
        } else {
            self.authHeader = nil
        }
    }

    public func request<T: Codable>(method: String, params: [AnyCodable] = [], completion: @escaping (Result<T, Error>) -> Void) {
        var req = URLRequest(url: url)
        req.httpMethod = "POST"
        req.setValue("application/json", forHTTPHeaderField: "Content-Type")
        if let auth = authHeader {
            req.setValue(auth, forHTTPHeaderField: "Authorization")
        }

        let payload = RPCRequest(jsonrpc: "1.0", id: "rtm-sdk-swift", method: method, params: params)
        do {
            req.httpBody = try JSONEncoder().encode(payload)
        } catch {
            completion(.failure(error))
            return
        }

        let task = URLSession.shared.dataTask(with: req) { data, response, error in
            if let error = error {
                completion(.failure(error))
                return
            }

            guard let data = data else {
                completion(.failure(NSError(domain: "RaptoreumClient", code: -1, userInfo: [NSLocalizedDescriptionKey: "No data received"])))
                return
            }

            do {
                let decoder = JSONDecoder()
                let rpcResp = try decoder.decode(RPCResponse<T>.self, from: data)
                if let error = rpcResp.error {
                    completion(.failure(error))
                } else if let result = rpcResp.result {
                    completion(.success(result))
                } else {
                    completion(.failure(NSError(domain: "RaptoreumClient", code: -2, userInfo: [NSLocalizedDescriptionKey: "No result or error found"])))
                }
            } catch {
                completion(.failure(error))
            }
        }
        task.resume()
    }

    public func validateaddress(address: String, completion: @escaping (Result<AnyCodable, Error>) -> Void) {
        request(method: "validateaddress", params: [AnyCodable(address)], completion: completion)
    }

    public func sendmany(amounts: [String: Double], minconf: Int = 1, comment: String = "", completion: @escaping (Result<String, Error>) -> Void) {
        request(method: "sendmany", params: [
            AnyCodable(""),
            AnyCodable(amounts),
            AnyCodable(minconf),
            AnyCodable(comment)
        ], completion: completion)
    }
}
