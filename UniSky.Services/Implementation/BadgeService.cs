using System.Globalization;
using Microsoft.Extensions.Logging;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace UniSky.Services;

public class BadgeService(ILogger<BadgeService> logger) : IBadgeService
{
    private readonly BadgeUpdater badgeManager = BadgeUpdateManager.CreateBadgeUpdaterForApplication();

    public void UpdateBadge(int badgeCount)
    {
        if (badgeCount == 0)
        {
            logger.LogDebug("Clearing badge.");

            badgeManager.Clear();
        }
        else
        {
            logger.LogDebug("Showing badge for {Count} notifications", badgeCount);

            var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
            var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
            badgeElement.SetAttribute("value", badgeCount.ToString(CultureInfo.InvariantCulture));

            badgeManager.Update(new BadgeNotification(badgeXml));
        }
    }
}
