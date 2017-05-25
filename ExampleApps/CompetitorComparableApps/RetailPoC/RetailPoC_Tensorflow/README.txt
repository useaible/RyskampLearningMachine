Greetings!

To execute the RetailPoC_Tensorflow project:

Required python version: 
	- Python 3.* 64-bit

Required python packages:
	- pythonnet (2.3.0)
	- pypiwin32 (219)
	- tensorflow (1.1.0)

For the DB connections, in case the Config file checker at program startup does not work (i.e, your Python installation folder was not found programatically for some reason) then
make sure to copy the `python.exe.config` (included in the project) file to your local Python (3.*) installation folder (the same place the `python.exe` is located).
By default, the connection strings are set to connect to SQL SERVER instance using the default server and credentials. 
If you have a different setting or server, please override the values inside the `python.exe.config` file.

**NOTE**
For the RLMConnStr, refrain from changing the Database value (i.e., Database={{dbName}}) as that is a placeholder used by the RLM engine. If you want to change the RLM database manually, 
you will have to do it programmatically by calling the RlmNetwork constructor which asks for a database name.

Thanks