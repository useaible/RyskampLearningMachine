import re
import sys
import os.path

class ConfigFileManager():
    pythonPath = None
    fileName = "python.exe.config"

    #Connection string settings
    rlm_server = "." # change this to your own server or leave as is if default works fine
    rlm_user_password= "Integrated Security=True" # if you specifically have user and password change this to "User=user_here;Password=password_here"

    planogram_server = "." # see comment above
    planogram_user_password = "Integrated Security=True" # see comment above

    configContent = """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <!--For more information on Entity Framework configuration, visit http://go.microsoft.com/fwlink/?LinkID=237468-->
    <section name="entityFramework" type="System.Data.Entity.Internal.ConfigFile.EntityFrameworkSection, EntityFramework, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" requirePermission="false" />
  </configSections>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.6.2" />
  </startup>
  <entityFramework>
    <defaultConnectionFactory type="System.Data.Entity.Infrastructure.LocalDbConnectionFactory, EntityFramework">
      <parameters>
        <parameter value="mssqllocaldb" />
      </parameters>
    </defaultConnectionFactory>
    <providers>
      <provider invariantName="System.Data.SqlClient" type="System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer" />
    </providers>
  </entityFramework>
  <appSettings>
    <add key="RLMConnStr" value="Server=""" + rlm_server + """;Database={{dbName}};""" + rlm_user_password + """;"/>
  </appSettings>
  <connectionStrings>
    <add name="PlanogramContext" providerName="System.Data.SqlClient" connectionString="Server=""" + planogram_server + """;Database=RetailPoC;""" + planogram_user_password + """;"/>
  </connectionStrings>
</configuration>"""
    
    def getPythonPath(self):
        pattern = re.compile(".+\\Python3\d$")
        for p in sys.path:
            result = pattern.match(p)
            if (not result == None):
                return result.string
        return None

    def configure(self):
        status = False
        print ("Checking config file...")      
        self.pythonPath = self.getPythonPath()

        if (self.pythonPath == None):
            print ("You do not have the correct version of Python installed or used for this project. Please use Python 3.*")
        else:
            filePath =self.pythonPath + "\\" + self.fileName
            if (not os.path.isfile(filePath)):
                file = open(filePath, "w");
                file.write(self.configContent)
                file.close()
                print ("Config file '" + self.fileName + "' created automatically and is located in '" + self.pythonPath + "'")
            else:
                print ("Config file '" + self.fileName + "'...OK")                
            status = True

        return status