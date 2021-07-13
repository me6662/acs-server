using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace ACSTestServer
{
    public partial class Form1 : Form
    {
        const int PORTNUM = 8112;

        // 전역변수
        Socket currentSocket;
        Thread tcpClientThread;
        ManualResetEvent allDone = new ManualResetEvent(false);
        Dictionary<string, Socket> clientList;

        int agvNum = 0;


        public Form1()
        {
            InitializeComponent();

            tcpClientThread = new Thread(new ParameterizedThreadStart(startListening));
            tcpClientThread.Start(PORTNUM);

            Console.WriteLine("[SYSTEM] Start Listen");

            tmrDisp.Enabled = true;

        }

        #region 통신관련
        
        public class StateObject
        {
            public Socket workSocket = null; // client socket
            public const int BUFFERSIZE = 1024;
            public byte[] buffer = new byte[BUFFERSIZE]; // receive buffer
            public StringBuilder sb = new StringBuilder(); //received data string.
        }

        // 통신 listen 스레드 함수
        private void startListening(object portNum) 
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, (int)portNum); // establish local endpoint for socket
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // create TCP/IP socket

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    allDone.Reset();
                    Console.WriteLine("[SYSTEM] Waiting for the TCP/IP connection...");
                    listener.BeginAccept(new AsyncCallback(acceptCallback), listener);
                    allDone.WaitOne();
                }
            } 
            catch (Exception e)
            {
                Console.WriteLine("[SYSTEM] Listen Error " + e.Message);
            }
        }

        // listen -> 연결 콜백
        private void acceptCallback(IAsyncResult ar)
        {
            allDone.Set();

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            string IPaddress = handler.RemoteEndPoint.ToString();

            Console.WriteLine("[SYSTEM] Client Access (IP :" + IPaddress + ")");

            StateObject state = new StateObject();
            state.workSocket = handler;
            currentSocket = handler;
            /*
             여기서 Client 추가된것 UI 표기
             */

            handler.BeginReceive(state.buffer, 0, StateObject.BUFFERSIZE, 0, new AsyncCallback(readCallback), state);
        }

        // readcallback --> 데이터 수신 콜백

        string acsMessage = string.Empty;

        private void readCallback(IAsyncResult ar)
        {
            string content = string.Empty;
            StateObject state = (StateObject)ar.AsyncState;

            Socket handler = state.workSocket;

            try
            {
                int bytesRead = handler.EndReceive(ar);

                if(bytesRead > 0)
                {
                    acsMessage += Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                    while (acsMessage.Contains("end"))
                    {
                        acsMessage = acsMessage.Substring(acsMessage.IndexOf("start")); // <MESSAGE> 시작 인덱스
                        string msgToParse = acsMessage.Substring(0, acsMessage.IndexOf("end") + 3); // <MESSAGE> ~ </MESSAGE>  데이터 


                        acsMessage = acsMessage.Substring(msgToParse.Length, acsMessage.Length - msgToParse.Length);// </MESSAGE> 뒤에 남는데이터 이어서 활용

                        Console.WriteLine("[RECEIVE] " + msgToParse);
                    }

                    state.sb.Clear();
                    handler.BeginReceive(state.buffer, 0, StateObject.BUFFERSIZE, 0, new AsyncCallback(readCallback), state);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[SYSTEM] Data Receive Error", e);
            }
        }

        private void sendCallBack(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
                
            }
            catch (Exception e)
            {
                Console.WriteLine("[SYSTEM] Data Sent Error ", e);
            }
        }

        private void send(Socket handler, string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(sendCallBack), handler);

            Console.WriteLine("[SEND] "+ data);
        }


        #endregion


        private void tmrDisp_Tick(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string sendData = textBox1.Text;
            
            if(sendData != string.Empty)
            {
                send(currentSocket,sendData);
            }
        }
    }
}
