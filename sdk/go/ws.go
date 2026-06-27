package raptoreum

import (
	"crypto/tls"
	"fmt"
	"net"
	"net/url"
	"strings"
)

type RaptoreumWebSocketClient struct {
	Url       string
	conn      net.Conn
	Connected bool
}

func NewRaptoreumWebSocketClient(wsUrl string) *RaptoreumWebSocketClient {
	return &RaptoreumWebSocketClient{
		Url:       wsUrl,
		Connected: false,
	}
}

func (ws *RaptoreumWebSocketClient) Connect(messageCallback func(string)) error {
	u, err := url.Parse(ws.Url)
	if err != nil {
		return err
	}

	host := u.Host
	if !strings.Contains(host, ":") {
		if u.Scheme == "wss" {
			host += ":443"
		} else {
			host += ":80"
		}
	}

	var conn net.Conn
	if u.Scheme == "wss" {
		conn, err = tls.Dial("tcp", host, nil)
	} else {
		conn, err = net.Dial("tcp", host)
	}
	if err != nil {
		return err
	}

	ws.conn = conn

	handshake := fmt.Sprintf(
		"GET %s HTTP/1.1\r\n"+
			"Host: %s\r\n"+
			"Upgrade: websocket\r\n"+
			"Connection: Upgrade\r\n"+
			"Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n"+
			"Sec-WebSocket-Version: 13\r\n\r\n",
		u.RequestURI(), u.Host,
	)

	_, err = conn.Write([]byte(handshake))
	if err != nil {
		conn.Close()
		return err
	}

	buf := make([]byte, 1024)
	n, err := conn.Read(buf)
	if err != nil {
		conn.Close()
		return err
	}

	if !strings.Contains(string(buf[:n]), "101 Switching Protocols") {
		conn.Close()
		return fmt.Errorf("handshake failed: %s", string(buf[:n]))
	}

	ws.Connected = true

	go func() {
		defer ws.Close()
		for ws.Connected {
			header := make([]byte, 2)
			_, err := conn.Read(header)
			if err != nil {
				break
			}

			opcode := header[0] & 0x0f
			masked := header[1] & 0x80
			payloadLen := uint64(header[1] & 0x7f)

			if payloadLen == 126 {
				lenBuf := make([]byte, 2)
				_, err = conn.Read(lenBuf)
				if err != nil {
					break
				}
				payloadLen = uint64(lenBuf[0])<<8 | uint64(lenBuf[1])
			} else if payloadLen == 127 {
				lenBuf := make([]byte, 8)
				_, err = conn.Read(lenBuf)
				if err != nil {
					break
				}
				payloadLen = 0
				for i := 0; i < 8; i++ {
					payloadLen = payloadLen<<8 | uint64(lenBuf[i])
				}
			}

			var mask []byte
			if masked != 0 {
				mask = make([]byte, 4)
				_, err = conn.Read(mask)
				if err != nil {
					break
				}
			}

			payload := make([]byte, payloadLen)
			read := uint64(0)
			for read < payloadLen {
				n, err := conn.Read(payload[read:])
				if err != nil {
					break
				}
				read += uint64(n)
			}

			if masked != 0 {
				for i := uint64(0); i < payloadLen; i++ {
					payload[i] ^= mask[i%4]
				}
			}

			if opcode == 8 {
				break
			}

			if opcode == 1 {
				messageCallback(string(payload))
			}
		}
	}()

	return nil
}

func (ws *RaptoreumWebSocketClient) Close() {
	ws.Connected = false
	if ws.conn != nil {
		ws.conn.Close()
	}
}
