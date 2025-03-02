using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit
{
    public class MagicSystemResponse
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }        
    }
    public class MagicSystemResponse<T> : MagicSystemResponse
    {
        public T? Result { get; set; } = default(T?);
    }
}
