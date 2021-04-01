// -----------------------------------------------------------------------
// <copyright file="AccountMigrationWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls;
using System;
using System.Linq;
using System.Windows;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for AccountMigrationWindow.xaml
    /// </summary>
    public partial class AccountMigrationWindow : MetroWindow
    {
        private bool _appliedChanges;

        public AccountMigrationWindow()
        {
            InitializeComponent();
            _appliedChanges = false;
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            string accountId = AccountIdTextBox.Text;
            if (String.IsNullOrEmpty(accountId))
            {
                MessageBox.Show("Cannot use an empty account.");
                return;
            }

            using (var context = new QpasDbContext())
            {
                //check if this account exists, otherwise add it
                Account account;
                if (context.Accounts.Any(x => x.AccountId == accountId))
                {
                    account = context.Accounts.First(x => x.AccountId == accountId);
                }
                else
                {
                    account = new Account { AccountId = accountId };
                    context.Accounts.Add(account);
                    context.SaveChanges();
                }

                //Now that we have the account, set it everywhere

                foreach (EquitySummary es in context.EquitySummaries)
                {
                    es.Account = account;
                }

                foreach (DividendAccrual da in context.DividendAccruals)
                {
                    da.Account = account;
                }

                foreach (Order o in context.Orders)
                {
                    o.Account = account;
                }

                foreach (Execution ex in context.Executions)
                {
                    ex.Account = account;
                }

                foreach (FXTransaction fxt in context.FXTransactions)
                {
                    fxt.Account = account;
                }

                foreach (FXPosition fxp in context.FXPositions)
                {
                    fxp.Account = account;
                }

                foreach (CashTransaction ct in context.CashTransactions)
                {
                    ct.Account = account;
                }

                foreach (OpenPosition op in context.OpenPositions)
                {
                    op.Account = account;
                }

                foreach (PriorPosition pp in context.PriorPositions)
                {
                    pp.Account = account;
                }

                context.SaveChanges();
            }
            _appliedChanges = true;
            MessageBox.Show("Success!");
            Close();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_appliedChanges)
            {
                var res = MessageBox.Show("Any entries with an empty account field cannot be used to generate performance reports. Are you sure you want to exit without setting an account?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}