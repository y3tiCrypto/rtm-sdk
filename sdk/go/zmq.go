package raptoreum

import (
	"encoding/binary"
	"fmt"
	"io"
	"net"
)

type RaptoreumZmqListener struct {
	Host    string
	Port    int
	conn    net.Conn
	running bool
}

func NewRaptoreumZmqListener(host string, port int) *RaptoreumZmqListener {
	return &RaptoreumZmqListener{
		Host: host,
		Port: port,
	}
}

func (z *RaptoreumZmqListener) Start(callback func(string, []byte)) error {
	addr := fmt.Sprintf("%s:%d", z.Host, z.Port)
	conn, err := net.Dial("tcp", addr)
	if err != nil {
		return err
	}
	z.conn = conn
	z.running = true

	// ZMTP 3.0 Signature
	sig := []byte{0xff, 0, 0, 0, 0, 0, 0, 0, 0, 0x7f}
	if _, err := conn.Write(sig); err != nil {
		conn.Close()
		return err
	}

	// ZMTP Greeting payload
	details := []byte{
		0x03, 0x00, // Version
		'N', 'U', 'L', 'L', 0, // Mechanism NULL
	}
	padding := make([]byte, 15)
	details = append(details, padding...)
	details = append(details, 0x00) // as-server

	if _, err := conn.Write(details); err != nil {
		conn.Close()
		return err
	}

	resp := make([]byte, 64)
	if _, err := io.ReadFull(conn, resp); err != nil {
		conn.Close()
		return err
	}

	// Ready command specifying socket type SUB
	ready := []byte{
		0x04,
		20,
		5, 'R', 'E', 'A', 'D', 'Y',
		11, 'S', 'o', 'c', 'k', 'e', 't', '-', 'T', 'y', 'p', 'e',
		0, 3, 'S', 'U', 'B',
	}
	if _, err := conn.Write(ready); err != nil {
		conn.Close()
		return err
	}

	// Subscription messages: [0x01 (Subscribe), topic]
	topics := []string{"rawtx", "rawblock", "hashblock", "hashtx"}
	for _, topic := range topics {
		payload := append([]byte{0x01}, []byte(topic)...)
		subCmd := []byte{
			0x00,
			byte(len(payload)),
		}
		subCmd = append(subCmd, payload...)
		if _, err := conn.Write(subCmd); err != nil {
			conn.Close()
			return err
		}
	}

	go func() {
		defer z.Stop()
		for z.running {
			flags := make([]byte, 1)
			if _, err := io.ReadFull(conn, flags); err != nil {
				break
			}
			
			var length uint64
			if flags[0] & 0x02 != 0 {
				lenBuf := make([]byte, 8)
				if _, err := io.ReadFull(conn, lenBuf); err != nil {
					break
				}
				length = binary.BigEndian.Uint64(lenBuf)
			} else {
				lenBuf := make([]byte, 1)
				if _, err := io.ReadFull(conn, lenBuf); err != nil {
					break
				}
				length = uint64(lenBuf[0])
			}

			payload := make([]byte, length)
			if _, err := io.ReadFull(conn, payload); err != nil {
				break
			}

			// Topic frame has MORE flag set (0x01)
			if flags[0] & 0x01 != 0 {
				topicStr := string(payload)
				
				nextFlags := make([]byte, 1)
				if _, err := io.ReadFull(conn, nextFlags); err != nil {
					break
				}
				
				var nextLen uint64
				if nextFlags[0] & 0x02 != 0 {
					lenBuf := make([]byte, 8)
					if _, err := io.ReadFull(conn, lenBuf); err != nil {
						break
					}
					nextLen = binary.BigEndian.Uint64(lenBuf)
				} else {
					lenBuf := make([]byte, 1)
					if _, err := io.ReadFull(conn, lenBuf); err != nil {
						break
					}
					nextLen = uint64(lenBuf[0])
				}
				
				body := make([]byte, nextLen)
				if _, err := io.ReadFull(conn, body); err != nil {
					break
				}
				
				callback(topicStr, body)

				if nextFlags[0] & 0x01 != 0 {
					thirdFlags := make([]byte, 1)
					if _, err := io.ReadFull(conn, thirdFlags); err != nil {
						break
					}
					var thirdLen uint64
					if thirdFlags[0] & 0x02 != 0 {
						lenBuf := make([]byte, 8)
						if _, err := io.ReadFull(conn, lenBuf); err != nil {
							break
						}
						thirdLen = binary.BigEndian.Uint64(lenBuf)
					} else {
						lenBuf := make([]byte, 1)
						if _, err := io.ReadFull(conn, lenBuf); err != nil {
							break
						}
						thirdLen = uint64(lenBuf[0])
					}
					thirdPayload := make([]byte, thirdLen)
					io.ReadFull(conn, thirdPayload)
				}
			}
		}
	}()

	return nil
}

func (z *RaptoreumZmqListener) Stop() {
	z.running = false
	if z.conn != nil {
		z.conn.Close()
	}
}
