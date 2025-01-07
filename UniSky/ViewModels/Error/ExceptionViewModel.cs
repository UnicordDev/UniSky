using System;

namespace UniSky.ViewModels.Error;

internal class ExceptionViewModel : ErrorViewModel
{
    public ExceptionViewModel(Exception ex)
    {
        if (ex.InnerException is Exception innerEx)
            this.Message = innerEx.Message;
        else
            this.Message = ex.Message;
    }
}
