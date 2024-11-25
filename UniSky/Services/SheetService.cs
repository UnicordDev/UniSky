using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Sheet;
using Windows.UI.Xaml;

namespace UniSky.Services
{
    internal class SheetService : ISheetService
    {
        private readonly SheetRootControl sheetRoot;

        public SheetService()
        {
            this.sheetRoot = Window.Current.Content.FindDescendant<SheetRootControl>();
        }

        public async Task ShowAsync<T>() where T : SheetControl, new()
        {
            var control = new T();
            sheetRoot.ShowSheet(control);
        }

        /// <summary>
        /// Tries to close the currently active sheet, returns a boolean to indicate if the sheet was hidden or not
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TryCloseAsync()
        {
            return await sheetRoot.HideSheetAsync();
        }
    }
}
