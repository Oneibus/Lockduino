// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SmartCardDetector.cs" company="Microsoft">
//   Copyright Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   The system tray UI component.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LockDuinoAPI
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// The smart card detector.
    /// </summary>
    public static class SmartCardDetector
    {
        #region Constants

        /// <summary>
        /// The smart card is absent flag.
        /// </summary>
        private const int ScardAbsent = 1;

        /// <summary>
        /// The smart card leave flag.
        /// </summary>
        private const int ScardLeaveCard = 0;

        /// <summary>
        /// The smart card protocol flag.
        /// </summary>
        private const int ScardProtocolTx = 3;

        /// <summary>
        /// The smart card user scope flag.
        /// </summary>
        private const int ScardScopeUser = 0;

        /// <summary>
        /// The smart card shared access flag.
        /// </summary>
        private const uint ScardShareShared = 2;

        /// <summary>
        /// The smart card success flag.
        /// </summary>
        private const int ScardSSuccess = 0;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The has badge.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool HasBadge()
        {
            bool status = false;
            IntPtr context;
            uint pcchReaders = 0;
            const char Nullchar = (char)0;
            var readersList = new ArrayList();

            // establish context
            SCardEstablishContext(ScardScopeUser, IntPtr.Zero, IntPtr.Zero, out context);

            // get readers buffer len
            SCardListReaders(context, IntPtr.Zero, null, ref pcchReaders);
            var mszReaders = new byte[pcchReaders];

            // fill readers' buffer
            SCardListReaders(context, IntPtr.Zero, mszReaders, ref pcchReaders);

            // fill readersList
            // remember that readers is a multistring with a double trailing \0
            // This is much easier and faster to do the allocation like this than the looping way
            var ascii = new ASCIIEncoding();
            var currbuff = ascii.GetString(mszReaders);
            var len = (int)pcchReaders;

            while (len > 0 && currbuff[0] != Nullchar)
            {
                int nullindex = currbuff.IndexOf(Nullchar);
                string reader = currbuff.Substring(0, nullindex);
                readersList.Add(reader);
                len = len - (reader.Length + 1);
                currbuff = currbuff.Substring(nullindex + 1, len);

                if (!reader.Contains("Virtual"))
                {
                    uint handle;
                    uint currProtocol;
                    var sts = SCardConnect(
                        context,
                        reader,
                        ScardShareShared,
                        ScardProtocolTx,
                        out handle,
                        out currProtocol);

                    if (sts != ScardSSuccess)
                    {
                        continue;
                    }

                    var state = 0;
                    uint protocol = 0;
                    var readerName = string.Empty;
                    var readerNameLen = 1024;
                    var bytesAtr = new byte[1024];
                    var blen = 1024;
                    sts = SCardStatus(
                        handle, 
                        readerName, 
                        ref readerNameLen, 
                        ref state, 
                        ref protocol, 
                        bytesAtr, 
                        ref blen);

                    if (sts == ScardSSuccess && state != ScardAbsent)
                    {
                        status = true;
                    }

                    SCardDisconnect(handle, ScardLeaveCard);
                }
            }

            SCardReleaseContext(context);
            return status;
        }

        /// <summary>
        /// Lists the smart card readers.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="groups">
        /// The groups.
        /// </param>
        /// <param name="readers">
        /// The readers.
        /// </param>
        /// <param name="lenReaders">
        /// The length of readers.
        /// </param>
        /// <returns>
        /// The <see cref="int"/> value.
        /// </returns>
        [DllImport("winscard.dll", EntryPoint = "SCardListReadersA", CharSet = CharSet.Ansi)]
        public static extern int SCardListReaders(IntPtr context, IntPtr groups, byte[] readers, ref uint lenReaders);

        #endregion

        #region Methods

        /// <summary>
        /// Connects to a smart card reader.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <param name="shareMode">
        /// The share Mode.
        /// </param>
        /// <param name="preferredProtocols">
        /// The preferred Protocols.
        /// </param>
        /// <param name="cardHandle">
        /// The card Handle.
        /// </param>
        /// <param name="activeProtocol">
        /// The active Protocol.
        /// </param>
        /// <returns>
        /// The <see cref="int"/> value.
        /// </returns>
        [DllImport("winscard.dll", EntryPoint = "SCardConnect", CharSet = CharSet.Auto)]
        private static extern int SCardConnect(IntPtr context, [MarshalAs(UnmanagedType.LPTStr)] string reader, uint shareMode, uint preferredProtocols, out uint cardHandle, out uint activeProtocol);

        /// <summary>
        /// Disconnects from the smart card reader.
        /// </summary>
        /// <param name="cardHandle">
        /// The card Handle.
        /// </param>
        /// <param name="disposition">
        /// The disposition.
        /// </param>
        /// <returns>
        /// The <see cref="int"/> value.
        /// </returns>
        [DllImport("winscard.dll")]
        private static extern int SCardDisconnect(uint cardHandle, int disposition);

        /// <summary>
        /// Establishes a smart card API context.
        /// </summary>
        /// <param name="scope">
        /// The scope.
        /// </param>
        /// <param name="reserved1">
        /// The reserved 1.
        /// </param>
        /// <param name="reserved2">
        /// The reserved 2.
        /// </param>
        /// <param name="contextHandle">
        /// The context Handle.
        /// </param>
        /// <returns>
        /// The <see cref="int"/> value.
        /// </returns>
        [DllImport("winscard.dll")]
        private static extern int SCardEstablishContext(uint scope, IntPtr reserved1, IntPtr reserved2, out IntPtr contextHandle);

        /// <summary>
        /// Releases a smart card API context.
        /// </summary>
        /// <param name="contextHandle">
        /// The context Handle.
        /// </param>
        /// <returns>
        /// The <see cref="int"/> value.
        /// </returns>
        [DllImport("winscard.dll")]
        private static extern int SCardReleaseContext(IntPtr contextHandle);

        /// <summary>
        /// Gets the smart card reader status.
        /// </summary>
        /// <param name="cardHandle">
        /// The card handle.
        /// </param>
        /// <param name="readerName">
        /// The reader name.
        /// </param>
        /// <param name="lenReaderName">
        /// The length of reader name.
        /// </param>
        /// <param name="state">
        /// The state.
        /// </param>
        /// <param name="protocol">
        /// The protocol.
        /// </param>
        /// <param name="cardAttributeBytes">
        /// The card attribute bytes.
        /// </param>
        /// <param name="lenCardAttributeBytes">
        /// The length of the card attribute bytes.
        /// </param>
        /// <returns>
        /// The <see cref="int"/> value.
        /// </returns>
        [DllImport("winscard.dll")]
        private static extern int SCardStatus(uint cardHandle, [MarshalAs(UnmanagedType.LPTStr)] string readerName, ref int lenReaderName, ref int state, ref uint protocol, byte[] cardAttributeBytes, ref int lenCardAttributeBytes);

        #endregion
    }
}