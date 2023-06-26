The testing infrastructure works for all storage backends GnuCash supports. 

To test against networked SQL databases, just spin up the database server, then tweak `Config.cs` accordingly.
The testing infrastructure will create databases automatically, the created databases are prefixed with "netcash~".

Some test cases for PostgreSQL might fail, from my limited finding, GnuCash seems to not close database connections
correctly for PostgreSQL when a session ends.
