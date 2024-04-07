#pragma warning disable CA2252, CA1416, CS8618, CS8600, CS8625
using server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    internal class ConnectionHandler
    {
        private readonly QuicConnection Connection;
        public Guid ClientUuid { get; private set; }

        public ConnectionHandler(QuicConnection connection)
        {
            this.Connection = connection;
        }

        public async Task<QuicStream> CreateStream()
        {
            return await this.Connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        }
        public async Task HandleConnectionAsync()
        {
            Console.WriteLine($"Client [{Connection.RemoteEndPoint}]: connected");

            try
            {
                // Continuously listen for and handle incoming streams
                while (true)
                {
                    await using var stream = await Connection.AcceptInboundStreamAsync();
                    await HandleStreamAsync(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during connection handling: {ex.Message}");
                bool removed = QuicHandler.ClientConnections.TryRemove(ClientUuid, out ConnectionHandler value);
                if (removed) {
                    Console.WriteLine("Connection lost, removed from mapping");
                }
                else {
                    Console.WriteLine("Unexpected error occured while attempting to remove connection");
                }
            }
        }

        public async Task HandleStreamAsync(QuicStream stream)
        {
            
                Console.WriteLine("Gonna read headers");
                var header = await ReadHeaderAsync(stream);
                Console.WriteLine("Headers read");
                if (header != null)
                {
                    Console.WriteLine("Headers are not null");
                    this.ClientUuid = header.SelfUuid;
                    Guid targetUuid = header.TargetUuid;
                    QuicHandler.ClientConnections[this.ClientUuid] = this;
                    Console.WriteLine(this.ClientUuid);
                    foreach (var kvp in QuicHandler.ClientConnections)
                    {
                        Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
                    }
                    if (header.Opcode != 15)
                    {
                        try
                        {
                            var targetConnection = QuicHandler.GetConnectionHandler(targetUuid);
                            Console.WriteLine("Found connection");
                            await HandleRelayMessageAsync(stream, header, targetConnection);
                            Console.WriteLine($"Relayed message from {header.SelfUuid} to {header.TargetUuid}");
                        }
                        catch (KeyNotFoundException ex)
                        {
                            await ReplyTargetNotFoundAsync(stream, header);
                            Console.WriteLine($"{ex.Message}");

                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine($"Error during relay handling: {ex.Message}");
                        }
                    }


                }
            

        }


        //This function is used to send data over a stream
        public async Task SendMessageAsync(QuicStream stream, MizukiHeader header, byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(header.Opcode);
                    writer.Write(header.SelfUuid.ToByteArray());
                    writer.Write(header.TargetUuid.ToByteArray());
                    writer.Write(header.DataLength);
                    if (data.Length != header.DataLength)
                    {
                        throw new ArgumentException("Data length does not match the specified DataLength in the message.");
                    }
                    if (data != null && data.Length > 0)
                    {
                        writer.Write(data);
                    }
                    // No need to close the writer here, as it's being disposed by the using block
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(stream);
            } // MemoryStream is disposed here, but all operations on it are done

            await stream.FlushAsync(); // Ensure all data is sent
        }

        //This function will be used to send only the header without data
        public async Task SendHeaderAsync(QuicStream stream, MizukiHeader header)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(header.Opcode);
                    writer.Write(header.SelfUuid.ToByteArray());
                    writer.Write(header.TargetUuid.ToByteArray());
                    writer.Write(header.DataLength);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(stream);
            } // MemoryStream is disposed here, but all operations on it are done

            await stream.FlushAsync(); // Ensure all data is sent
        }
        public async Task HandleRelayMessageAsync(QuicStream stream, MizukiHeader header, ConnectionHandler targetConnectionHandler)
        {
            try
            {
                var targetStream = await targetConnectionHandler.CreateStream();
                Console.WriteLine("Created Stream");
                byte[] data = await ReadDataAsync(stream, header.DataLength);
                Console.WriteLine("Data Read");
                await SendMessageAsync(targetStream, header, data);
                Console.WriteLine("Message Sent");
                var responseHeader = await ReadHeaderAsync(targetStream);
                Console.WriteLine("Response Header Read");
                if (responseHeader != null)
                {
                    Console.WriteLine("Response Header is not null");
                    if (responseHeader.Opcode != 4)
                    {
                        Console.WriteLine("Response Header is not 4");
                        if (responseHeader.SelfUuid == header.TargetUuid && responseHeader.TargetUuid == header.SelfUuid)
                        {
                            var responseData = await ReadDataAsync(targetStream, responseHeader.DataLength);
                            Console.WriteLine("Response Data Read");
                            await targetStream.DisposeAsync();
                            await SendMessageAsync(stream, responseHeader, responseData);
                            Console.WriteLine("Response Data Sent");
                        }
                        else
                        {
                            throw new Exception("Invalid response");
                        }
                    }
                    else
                    {
                        await targetStream.DisposeAsync();
                        await SendHeaderAsync(stream, responseHeader);
                    }
                }
                else
                {
                    throw new Exception("Null Header");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while handling relay: {ex.Message}");
            }

        }

        public async Task ReplyTargetNotFoundAsync(QuicStream stream, MizukiHeader header)
        {

            var notFoundHeader = new MizukiHeader
            {
                Opcode = 0,
                SelfUuid = QuicHandler.OwnUuid, // Use server's UUID
                TargetUuid = header.SelfUuid,
                DataLength = 0
            };
            await SendHeaderAsync(stream, notFoundHeader);
        }

        public async Task<MizukiHeader> ReadHeaderAsync(QuicStream stream)
        {
            // Adjust the buffer size to accommodate the new header structure
            byte[] buffer = new byte[1 + 16 + 16 + 4]; // 1 byte for Opcode, 16 bytes for SelfUuid, 16 bytes for TargetUuid, 4 bytes for DataLength
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead < buffer.Length) throw new Exception("Stream ended before all header data could be read.");

            return new MizukiHeader
            {
                Opcode = buffer[0],
                SelfUuid = new Guid(new ReadOnlySpan<byte>(buffer, 1, 16)),
                TargetUuid = new Guid(new ReadOnlySpan<byte>(buffer, 17, 16)),
                DataLength = BitConverter.ToUInt32(buffer, 33)
            };
        }

        public async Task<byte[]> ReadDataAsync(QuicStream stream, uint dataLength)
        {
            byte[] dataBuffer = new byte[dataLength];
            await stream.ReadAsync(dataBuffer, 0, dataBuffer.Length); // Read the remaining data
            return dataBuffer;
        }
        public async Task<(Guid targetUuid, byte[] data)> ReadTargetUuidAndDataAsync(QuicStream stream, uint dataLength)
        {
            byte[] uuidBuffer = new byte[16];
            byte[] dataBuffer = new byte[dataLength];

            await stream.ReadAsync(uuidBuffer, 0, uuidBuffer.Length); // Read the target UUID
            await stream.ReadAsync(dataBuffer, 0, dataBuffer.Length); // Read the remaining data

            return (new Guid(uuidBuffer), dataBuffer);
        }


    }
}
