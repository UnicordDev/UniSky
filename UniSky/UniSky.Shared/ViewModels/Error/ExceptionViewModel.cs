using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniSky.ViewModels.Error;

internal class ExceptionViewModel : ErrorViewModel
{
    public ExceptionViewModel(Exception ex)
    {
        if (ex.InnerException is Exception innerEx)
            this.Message = innerEx.StackTrace ?? innerEx.Message;
        else
            this.Message = ex.StackTrace ?? ex.Message;
    }
}
