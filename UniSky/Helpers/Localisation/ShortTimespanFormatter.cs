using System.Diagnostics;
using Humanizer.Localisation;
using Humanizer.Localisation.Formatters;
using Windows.ApplicationModel.Resources;

namespace UniSky.Helpers.Localisation;

internal class ShortTimespanFormatter : DefaultFormatter
{
    private ResourceLoader _strings;

    public ShortTimespanFormatter(string localeCode) : base(localeCode)
    {
        _strings = ResourceLoader.GetForViewIndependentUse();
    }

    public override string TimeSpanHumanize(TimeUnit timeUnit, int unit, bool toWords = false)
    {
        Debug.Assert(toWords == false);

        return string.Concat(unit, _strings.GetString("TimeUnit_" + timeUnit.ToString()));
    }
}
