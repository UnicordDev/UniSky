using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniSky.Services;

#nullable enable

public interface ICdnUrlService
{
    public string? ProcessCdnUrl(string? url);
}
