﻿using LdapForNet.Utils;
using Xunit;

namespace LdapForNetTests.Utils
{
    public class LdapSidConverterTests
    {
        [Fact]
        public void LdapSidConverter_ConvertToHex_Return_String_In_Hex_Format()
        {
            var actual = LdapSidConverter.ConvertToHex("S-1-5-21-2127521184-1604012920-1887927527-72713");
            Assert.Equal("010500000000000515000000A065CF7E784B9B5FE77C8770091C0100", actual);
        }


        [Theory]
        [InlineData(new byte[] { 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x15, 0x00, 0x00, 0x00, 0xB0, 0x68, 0x77, 0x23, 0x28, 0xE5, 0x17, 0xDF, 0xDE, 0x78, 0x25, 0x94, 0x86, 0x13, 0x00, 0x00 }, "S-1-5-21-595028144-3742885160-2485483742-4998")]
        [InlineData(new byte[] { 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x15, 0x00, 0x00, 0x00, 0xA0, 0x65, 0xCF, 0x7E, 0x78, 0x4B, 0x9B, 0x5F, 0xE7, 0x7C, 0x87, 0x70, 0x09, 0x1C, 0x01, 0x00 }, "S-1-5-21-2127521184-1604012920-1887927527-72713")]
        [InlineData(new byte[] { 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x15, 0x00, 0x00, 0x00, 0x0C, 0xB1, 0x35, 0x5D, 0xFD, 0xC1, 0x22, 0x2A, 0x0D, 0x27, 0x8E, 0xD3, 0x06, 0x1E, 0x00, 0x00 }, "S-1-5-21-1563799820-706920957-3549308685-7686")]
        public void LdapSidConverter_ParseFromBytes_Convert_SID_Bytes_To_String(byte[] sid, string expected)
        {
	        var actual = LdapSidConverter.ParseFromBytes(sid);
            Assert.Equal(expected, actual);
        }
    }
}