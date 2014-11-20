using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Notification.PixPdq.Configuration;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using NHapi.Model.V231.Message;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Queue
{
    /// <summary>
    /// HL7 Message Queue (singleton)
    /// </summary>
    public class Hl7MessageQueue
    {
        /// <summary>
        /// Instance
        /// </summary>
        private static Hl7MessageQueue s_instance;
        /// <summary>
        /// Object
        /// </summary>
        private static Object s_lockObject = new Object();
        /// <summary>
        /// A message queue work item
        /// </summary>
        private Queue<MessageQueueWorkItem> m_tryQueue = new Queue<MessageQueueWorkItem>();

        /// <summary>
        /// Restore class
        /// </summary>
        [XmlRoot("queue")]
        public class MessageQueueWorkItemCollection
        {
            /// <summary>
            /// Work items
            /// </summary>
            [XmlElement("queueItem")]
            public List<MessageQueueWorkItem> WorkItems { get; set; }

            /// <summary>
            /// Work item collection ctor
            /// </summary>
            public MessageQueueWorkItemCollection()
            {

            }

            /// <summary>
            /// Source of the work items
            /// </summary>
            /// <param name="source"></param>
            public MessageQueueWorkItemCollection(Queue<MessageQueueWorkItem> source)
            {
                this.WorkItems = new List<MessageQueueWorkItem>(source);
            }
        }

        /// <summary>
        /// A work item for the at ID src queue
        /// </summary>
        [XmlRoot("queueItem")]
        public class MessageQueueWorkItem
        {

            /// <summary>
            /// Message text
            /// </summary>
            [XmlText]
            public String MessageText
            {
                get
                {
                    return new PipeParser().Encode(this.Message);
                }
                set
                {
                    this.Message = new PipeParser().Parse(value);
                }
            }

            /// <summary>
            /// Gets the message to be sent
            /// </summary>
            [XmlIgnore]
            public IMessage Message { get; private set; }

            /// <summary>
            /// Gets the target
            /// </summary>
            [XmlIgnore]
            public TargetConfiguration Target { get; set; }

            /// <summary>
            /// Gets or sets the name of the target
            /// </summary>
            [XmlAttribute("targetName")]
            public String TargetName
            {
                get
                {
                    return this.Target.Name;
                }
                set
                {
                    this.Target = PixNotifier.s_configuration.Targets.Find(o => o.Name == value);
                }
            }

            /// <summary>
            /// Fail count
            /// </summary>
            [XmlAttribute("failCount")]
            public int FailCount { get; set; }

            /// <summary>
            /// Serializer ctor
            /// </summary>
            public MessageQueueWorkItem()
            {

            }

            /// <summary>
            /// Creates a new message queue work item
            /// </summary>
            public MessageQueueWorkItem(TargetConfiguration target, IMessage message)
            {

                this.Target = target;
                this.Message = message;
            }

            /// <summary>
            /// Try to send the message
            /// </summary>
            /// <returns></returns>
            public bool TrySend()
            {

                try
                {
                    // Now send
                    MllpMessageSender sender = new MllpMessageSender(new Uri(this.Target.ConnectionString), this.Target.LlpClientCertificate, this.Target.TrustedIssuerCertificate);
                    ACK response = sender.SendAndReceive(this.Message) as ACK;
                    // See if the ACK is good
                    if (response == null)
                    {
                        this.FailCount += 1;
                        return false;
                    }

                    if (response.MSA.AcknowledgementCode.Value != "AA" &&
                        response.MSA.AcknowledgementCode.Value != "CA")
                    {
                        this.FailCount += 1;
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    this.FailCount += 1;
                    Trace.TraceError(e.ToString());
                    return false;
                }
            }

        }

        /// <summary>
        /// Private ctor
        /// </summary>
        private Hl7MessageQueue()
        {

        }

        /// <summary>
        /// Get or create the current queue item
        /// </summary>
        public static Hl7MessageQueue Current
        {
            get
            {
                if (s_instance == null)
                    lock (s_lockObject)
                        if (s_instance == null)
                            s_instance = new Hl7MessageQueue();
                return s_instance;
            }
        }

        /// <summary>
        /// Enqueue a message item
        /// </summary>
        public void EnqueueMessageItem(MessageQueueWorkItem item)
        {
            lock (s_lockObject)
                this.m_tryQueue.Enqueue(item);
        }

        /// <summary>
        /// Dequeue a message item
        /// </summary>
        public MessageQueueWorkItem DequeueMessageItem()
        {
            lock (s_lockObject)
                if (this.m_tryQueue.Count == 0)
                    return null;
                else
                    return this.m_tryQueue.Dequeue();
        }

        /// <summary>
        /// Dispose the queue (write to disk)
        /// </summary>
        public void Flush()
        {
            if (this.m_tryQueue.Count > 0)
            {
                lock (s_lockObject)
                    try
                    {
                        XmlSerializer xsz = new XmlSerializer(typeof(MessageQueueWorkItemCollection));
                        xsz.Serialize(File.Create(this.GetQueueFileName()), new MessageQueueWorkItemCollection(this.m_tryQueue));
                        this.m_tryQueue.Clear();
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError(e.ToString());
                    }
            }
        }

        /// <summary>
        /// Initialize the notifier
        /// </summary>
        public void Restore()
        {
            if (File.Exists(this.GetQueueFileName()))
            {
                try
                {
                    // TODO: Load queue here
                    XmlSerializer xsz = new XmlSerializer(typeof(MessageQueueWorkItemCollection));
                    using (FileStream fs = File.OpenRead(this.GetQueueFileName()))
                    {
                        var collection = xsz.Deserialize(fs) as MessageQueueWorkItemCollection;
                        lock (s_lockObject)
                            this.m_tryQueue = new Queue<MessageQueueWorkItem>(collection.WorkItems);
                    }
                    File.Delete(this.GetQueueFileName());
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
            }
        }

        /// <summary>
        /// Get the filename of the queue
        /// </summary>
        private String GetQueueFileName()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "hl7queue.xml");
        }

    }
}
