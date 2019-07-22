using System.Collections.Generic;

namespace LdapForNet
{
    public abstract class DirectoryResponse
    {
    }
    
    public class SearchResponse : DirectoryResponse
    {
        public List<LdapEntry> Entries { get; internal set; } = new List<LdapEntry>();
    }

    public class AddResponse : DirectoryResponse
    {
        
    }
}