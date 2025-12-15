using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPReceive : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5052;
    public bool startReceiving = true; 
    public string data;

    public void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        try 
        {
            client = new UdpClient(port);
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

            while (startReceiving)
            {
                try
                {
                    // 1. Block until we get at least one packet
                    byte[] buffer = client.Receive(ref anyIP);

                    // 2. LAG FIX: Drain the buffer!
                    while (client.Available > 0)
                    {
                        buffer = client.Receive(ref anyIP);
                    }

                    // 3. Convert only the latest packet to string
                    data = Encoding.UTF8.GetString(buffer);
                }
                catch (Exception) 
                {
                    // Socket closed or error, just ignore to prevent spam
                }
            }
        }
        catch (Exception e)
        {
            print(e.ToString());
        }
    }

    void OnApplicationQuit()
    {
        startReceiving = false;
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}