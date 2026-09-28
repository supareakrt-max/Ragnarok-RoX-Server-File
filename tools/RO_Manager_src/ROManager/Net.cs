using System;
using System.Net.Sockets;

namespace ROManager;

internal static class Net
{
	public static bool PortOpen(string host, int port, int ms = 400)
	{
		try
		{
			using TcpClient tcpClient = new TcpClient();
			IAsyncResult asyncResult = tcpClient.BeginConnect(host, port, null, null);
			bool flag = asyncResult.AsyncWaitHandle.WaitOne(ms) && tcpClient.Connected;
			if (flag)
			{
				tcpClient.EndConnect(asyncResult);
			}
			return flag;
		}
		catch
		{
			return false;
		}
	}
}
