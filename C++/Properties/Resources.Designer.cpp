#include "Resources.Designer.h"

namespace KH2FM_Toolkit
{
	namespace Properties
	{
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//		using namespace System;

		Resources::Resources()
		{
		}

		const System::Resources::ResourceManager &Resources::getResourceManager() const
		{
			if (object::ReferenceEquals(resourceMan, 0))
			{
				System::Resources::ResourceManager *temp = new System::Resources::ResourceManager("KH2FM_Toolkit.Properties.Resources", Resources::typeid::Assembly);
				resourceMan = temp;
			}
			return resourceMan;
		}

		const System::Globalization::CultureInfo &Resources::getCulture() const
		{
			return resourceCulture;
		}

		void Resources::setCulture(const System::Globalization::CultureInfo &value)
		{
			resourceCulture = value;
		}

		const unsigned char &Resources::getHashList() const
		{
			object *obj = getResourceManager()->GetObject("HashList", resourceCulture);
			return (static_cast<unsigned char[]>(obj));
		}

		const std::string &Resources::getReadme() const
		{
			return getResourceManager()->GetString("Readme", resourceCulture);
		}
	}
}
