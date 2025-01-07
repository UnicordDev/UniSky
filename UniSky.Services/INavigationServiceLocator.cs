namespace UniSky.Services;

public interface INavigationServiceLocator
{
    INavigationService GetNavigationService(string name);
}