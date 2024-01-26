﻿using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace ScreenShotLambda.Interfaces
{
    // 異常重試介面
    public interface IErrorRetry
    {
        /// <summary>
        /// 有錯誤的 queue 進行重試
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        Task retryErrorQueue(SQSEvent.SQSMessage message, ILambdaContext context);
    }
}