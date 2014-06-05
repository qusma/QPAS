QUSMA Performance Analysis Suite
====

QPAS is a trading journal with detailed performance, risk, and execution analytics, designed for Interactive Brokers clients. 

* Installer
* [Documentation](http://qusma.com/qpasdocs/index.php/Main_Page)

While QPAS currently only supports statements from Interactive Brokers, it can easily be extended to support other brokers. See [here](http://qusma.com/qpasdocs/index.php/Implementing_a_Statement_Parser) for more details.

Features:
------------------------
* Highly detailed performance statistics
* Ex-post risk analytics
* Benchmarking
* Execution analytics
* Trade journal: annotate trades with rich text and images

Screenshots:
------------------------
* Performance statistics
* Trade statistics
* Trade Overview
* Trade Notes
* Value at Risk and Expected Shortfall
* Monte Carlo Simulation with Drawdown and Ratio Distributions
* Maximum Favorable/Adverse Excursion
* Benchmarking
* Execution analysis
* Charting with entries and exits

Requirements:
------------------------
* [MySQL](http://dev.mysql.com/downloads/mysql/)/[MariaDB](https://downloads.mariadb.org/) or [SQL Server (2008+)](http://www.microsoft.com/en-us/download/details.aspx?id=29062)
* [.NET 4.5.1](http://www.microsoft.com/en-us/download/details.aspx?id=40773)
* [QDMS](http://qusma.com/software/qdms) (Needed only for some features: benchmarks, execution analysis, and charting)

Contributing:
------------------------
Simply send a pull request with your changes. If you're looking for something to do, the issue tracker has several bugs and planned features.
