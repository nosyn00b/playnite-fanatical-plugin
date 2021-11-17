using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanaticalLibrary
{
    public class FanaticalLibraryClient : LibraryClient
    {
        public override bool IsInstalled => false;

        public override void Open()
        {
            throw new NotImplementedException();
        }
    }
}