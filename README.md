QUSMA Performance Analytics Suite
====

QPAS is a trading journal with detailed performance, risk, and execution analytics, designed for Interactive Brokers clients. For an overview of its main features, check out the [performance report](http://qusma.com/qpasdocs/index.php/Performance_Report) page in the documentation.

* [Installer](http://qusma.com/QPAS/setup.exe)
* [Documentation](http://qusma.com/qpasdocs/index.php/Main_Page)

While QPAS currently only supports statements from Interactive Brokers, it can easily be extended to support other brokers. See [here](http://qusma.com/qpasdocs/index.php/Implementing_a_Statement_Parser) for more details.

For bug reports, feature requests, etc. use either the GitHub issue tracker or the [google group](https://groups.google.com/forum/#!forum/qusma-pas).

Features:
------------------------
* Highly detailed performance statistics
* Ex-post risk analytics
* Benchmarking
* Execution analytics
* Trade journal: annotate trades with rich text and images

Screenshots:
------------------------
* [Performance statistics](http://i.imgur.com/RhPRa6A.png)
* [Trade statistics](http://i.imgur.com/MmEXyw2.png)
* [Trade Overview](http://i.imgur.com/OfNf2cF.png)
* [Trade Notes](http://i.imgur.com/MM15ZiF.png)
* [Ex-post Value at Risk and Expected Shortfall](http://i.imgur.com/kEtqDSM.png)
* [Monte Carlo Simulation with Drawdown and Ratio Distributions](http://i.imgur.com/2W3jRXa.png)
* [Maximum Favorable/Adverse Excursion](http://i.imgur.com/hnkfe1a.png)
* [Benchmarking](http://i.imgur.com/Nnitn1M.png)
* [Execution analytics](http://i.imgur.com/YyXAFPk.png)
* [Charting with entries and exits](http://i.imgur.com/x2NmiTv.png)

Requirements:
------------------------
* [MySQL](http://dev.mysql.com/downloads/mysql/)/[MariaDB](https://downloads.mariadb.org/) or [SQL Server (2008+)](http://www.microsoft.com/en-us/download/details.aspx?id=29062)
* [.NET 4.5.1](http://www.microsoft.com/en-us/download/details.aspx?id=40773)
* [QDMS](http://qusma.com/software/qdms) (Needed only for some features: benchmarks, execution analysis, and charting)

Contributing:
------------------------
Simply send a pull request with your changes. If you're looking for something to do, the issue tracker has several bugs and planned features.
