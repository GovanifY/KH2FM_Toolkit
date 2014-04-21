#include "KH2FM_Toolkit.Properties.Settings.h"

namespace KH2FM_Toolkit
{
	namespace Properties
	{

Settings *Settings::defaultInstance = (static_cast<Settings*>(System::Configuration::ApplicationSettingsBase::Synchronized(new Settings())));

		const KH2FM_Toolkit::Properties::Settings &Settings::getDefault() const
		{
			return defaultInstance;
		}
	}
}
