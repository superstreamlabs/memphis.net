using System.Text;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using Memphis.Client.Models.Request;
using NATS.Client;

namespace Memphis.Client.Core
{
    public sealed class MemphisMessage
    {
        private readonly Msg _msg;
        private readonly MemphisClient _memphisClient;
        private readonly string _consumerGroup;
        private readonly int _macAckTimeMs;

        public MemphisMessage(Msg msgItem, MemphisClient memphisClient, string consumerGroup, int macAckTimeMs)
        {
            this._msg = msgItem;
            this._memphisClient = memphisClient;
            this._consumerGroup = consumerGroup;
            this._macAckTimeMs = macAckTimeMs;
        }

        public void Ack()
        {
            try
            {
                this._msg.AckSync(_macAckTimeMs);
            }
            catch (System.Exception e)
            {
                if (_msg.Header["$memphis_pm_id"] != null)
                {
                    var msgToAckModel = new PmAckMsg
                    {
                        Id = _msg.Header["$memphis_pm_id"],
                        ConsumerGroupName = _consumerGroup,
                    };
                    var msgToAckJson = JsonSerDes.PrepareJsonString<PmAckMsg>(msgToAckModel);

                    byte[] msgToAckBytes = Encoding.UTF8.GetBytes(msgToAckJson);
                    _memphisClient.BrokerConnection.Publish(
                        MemphisSubjects.PM_RESEND_ACK_SUBJ, msgToAckBytes);
                }

                throw new MemphisConnectionException("Unable to ack message", e);
            }
        }

        public byte[] GetData()
        {
            return _msg.Data;
        }

        public MsgHeader GetHeaders()
        {
            return _msg.Header;
        }
    }
}