using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var clientesConectados = new List<WebSocket>();

app.UseWebSockets();

app.Map("/ws", async context =>
{
	if (context.WebSockets.IsWebSocketRequest)
	{
		var webSocket = await context.WebSockets.AcceptWebSocketAsync();
		clientesConectados.Add(webSocket);
		await ReceberMensagens(webSocket);
	}
	else
	{
		context.Response.StatusCode = 400;
	}
});

async Task ReceberMensagens(WebSocket webSocket)
{
	var buffer = new byte[1024 * 4];

	while (webSocket.State == WebSocketState.Open)
	{
		Array.Clear(buffer, 0, buffer.Length);
		var resultadoRecebimento = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

		if (resultadoRecebimento.MessageType == WebSocketMessageType.Text)
		{
			var mensagemRecebida = Encoding.UTF8.GetString(buffer, 0, resultadoRecebimento.Count);
			Console.WriteLine($"Mensagem recebida do cliente: {mensagemRecebida}");

			foreach (var cliente in clientesConectados)
			{
				if (cliente != webSocket && cliente.State == WebSocketState.Open)
				{
					var bufferMensagem = Encoding.UTF8.GetBytes(mensagemRecebida);
					await cliente.SendAsync(new ArraySegment<byte>(bufferMensagem, 0, bufferMensagem.Length), WebSocketMessageType.Text, true, CancellationToken.None);
				}
			}
		}
		else if (resultadoRecebimento.MessageType == WebSocketMessageType.Close)
		{
			break;
		}
	}

	clientesConectados.Remove(webSocket);
}

app.Run();
