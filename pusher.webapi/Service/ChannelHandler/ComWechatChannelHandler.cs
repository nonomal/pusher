﻿using pusher.webapi.Common;
using pusher.webapi.Models;
using pusher.webapi.Service.ChannelHandler.ComWechat;
using pusher.webapi.Service.Database;

namespace pusher.webapi.Service.ChannelHandler;

/// <summary>
///     钉钉处理信息的方法
/// </summary>
public class ComWechatChannelHandler : IChannelHandler
{
    private readonly ILogger<ComWechatChannelHandler> _logger;
    private readonly Repository<ChannelMessageHistory> _repChannelMessageHistory;
    private readonly Repository<Message> _repMessage;


    public ComWechatChannelHandler(ILogger<ComWechatChannelHandler> logger,
        Repository<ChannelMessageHistory> repChannelMessageHistory, Repository<Message> repMessage)
    {
        _logger = logger;
        _repChannelMessageHistory = repChannelMessageHistory;
        _repMessage = repMessage;
    }

    /// <inheritdoc />
    public bool CanHandle(ChannelEnum channelType)
    {
        return channelType == ChannelEnum.ComWechat;
    }

    /// <inheritdoc />
    public async Task<bool> Handle(string url, ChannelMessageHistory channelMessageHistory)
    {
        var message = await _repMessage.GetByIdAsync(channelMessageHistory.MessageId);
        if (message.MessageType == MessageEnum.Text)
        {
            var data = new ComWechatText { Content = new ComWechatTextContent { Text = message.Content } };
            var httpClient = new HttpClient();
            var httpResponseMessage = await httpClient.PostAsJsonAsync(url, data);
            var result = await httpResponseMessage.Content.ReadFromJsonAsync<ComWechatResponse>();
            channelMessageHistory.Status = ChannelMessageStatus.Done;
            channelMessageHistory.Result = result?.ErrorMessage ?? string.Empty;
            if (result?.ErrorCode != 0)
            {
                _logger.LogWarning($"请求{message.Id}在{nameof(ComWechatChannelHandler)}发送失败");
            }
            else
            {
                _logger.LogInformation($"请求{message.Id}在{nameof(ComWechatChannelHandler)}发送成功");
                channelMessageHistory.Success = true;
            }

            await _repChannelMessageHistory.UpdateAsync(channelMessageHistory);
        }

        return true;
    }
}
