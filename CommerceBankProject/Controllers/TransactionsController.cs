﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CommerceBankProject.Data;
using CommerceBankProject.Models;
using System.Security.Claims;
using ClosedXML.Excel;

namespace CommerceBankProject.Controllers
{
    public class TransactionsController : Controller
    {

        private readonly CommerceBankDbContext _context;

        public TransactionsController(CommerceBankDbContext context)
        {
            _context = context;
        }


        // GET: Transactions
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            string userID = claim.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userID);

            IQueryable<Transaction> transactionIQ = from t in _context.Transaction.Where(
                t => t.customerID == user.customerID).OrderByDescending(d => d.onDate)
                                                    select t;

            List<Transaction> transactionList = await transactionIQ.AsNoTracking().ToListAsync();

            List<AccountRecord> actList = transactionList
                .GroupBy(p => p.actID)
                .Select(g => g.First())
                .Select(x => new AccountRecord { actID = x.actID, actType = x.actType })
                .ToList();

            TIndexViewModel vmod = new TIndexViewModel(
                transactions: transactionList,
                start: transactionList.LastOrDefault().onDate,
                end: transactionList.FirstOrDefault().onDate,
                accounts: actList);

            return View(vmod);
        }


        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }


        [Authorize]
        public async Task<IActionResult> FilterIndex(string actFilter, string descFilter, string fromDate, string toDate, string pageNumber)
        {
            // Get user claim
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            string userID = claim.Value;
            var user = await _context.Users.Where(u => u.Id == userID).FirstOrDefaultAsync();

            // Parse date strings into date objects
            DateTime tDate = DateTime.Parse(toDate).AddDays(1);
            
            DateTime fDate = DateTime.Parse(fromDate);

            IQueryable<Transaction> transactionIQ = from t in _context.Transaction.Where(
                t => t.customerID == user.customerID
                && t.onDate >= fDate
                && t.onDate <= tDate).OrderByDescending(d => d.onDate)
                                                    select t;
            List<AccountRecord> actList = transactionIQ.AsNoTracking().ToList()
                .GroupBy(p => p.actID)
                .Select(g => g.First())
                .Select(x => new AccountRecord { actID = x.actID, actType = x.actType })
                .ToList();

            if (actFilter != "all")
            {
                transactionIQ = transactionIQ.Where(t => t.actID == actFilter);

            }
            if (!string.IsNullOrEmpty(descFilter))
            {
                transactionIQ = transactionIQ.Where(t => t.description.Contains(descFilter));
            }

            List<Transaction> tList = await transactionIQ.AsNoTracking().ToListAsync();

            tDate = DateTime.Parse(toDate);

            TIndexViewModel vmod = new TIndexViewModel(
                accounts: actList,
                transactions: tList,
                start: fDate,
                end: tDate,
                desc: descFilter,
                page: int.Parse(pageNumber),
                acct: actFilter);


            return View("Index", vmod);
        }


        // GET: Transactions/Details/
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transaction
                .FirstOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }


        // GET: Transactions/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            string userID = claim.Value;
            var user = await _context.Users.Where(u => u.Id == userID).FirstOrDefaultAsync();
            string actQuery = "Select distinct actID, actType from [Transaction] where customerID = {0}";
            List<AccountRecord> actListSetup = await _context.Account.FromSqlRaw(actQuery, user.customerID).ToListAsync();
            UserTransactions t = new UserTransactions();
            Transaction trans = new Transaction();
            t.userAccounts = actListSetup;
            t.transaction = trans;
            //trans.transType = "CR";
            return View("Create", t);

        }

        // POST: Transactions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string tranActFilter, string transType, decimal amount, string description, string category)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            string userID = claim.Value;
            var user = await _context.Users.Where(u => u.Id == userID).FirstOrDefaultAsync();
            Transaction t = new Transaction();

            t.customerID = user.customerID;
            t.actID = tranActFilter;
            t.transType = transType;
            t.amount = amount;
            t.description = description;
            t.userEntered = true;
            t.category = category;
            t.onDate = DateTime.Now;


            string actBalance = "Select top 1 * from [Transaction] where customerID = {0} and actID = {1} order by onDate desc";

            Transaction prevTopTransaction = await _context.Transaction.FromSqlRaw(actBalance, user.customerID, t.actID).FirstOrDefaultAsync();


            decimal userBalance = prevTopTransaction.balance;
            t.actType = prevTopTransaction.actType;

            if (t.transType == "DR")
            {
                t.category = category;
                t.balance = userBalance - t.amount;
            }
            else
            {
                t.category = "Income";
                t.balance = userBalance + t.amount;
            }

                _context.Add(t);
                await _context.SaveChangesAsync();
                NotificationsController temp = new NotificationsController(_context);
                await temp.GenerateOnInsertion(user.customerID);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            
        }
            
        


        // GET: Transactions/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transaction
                .FirstOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transaction.FindAsync(id);
            _context.Transaction.Remove(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost, ActionName("Export")]
        public async Task<IActionResult> Export()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            string userID = claim.Value;
            var user = await _context.Users.Where(u => u.Id == userID).FirstOrDefaultAsync();

            var transactions = await _context.Transaction.Where(t => t.customerID == user.customerID).ToListAsync();

            DataTable dt = new DataTable("Transactions");
            dt.Columns.AddRange(new DataColumn[8] 
            { 
                new DataColumn("Account ID"),
                new DataColumn("Account Type"),
                new DataColumn("Date"),
                new DataColumn("Description"),
                new DataColumn("Category"),
                new DataColumn("Transaction Type"),
                new DataColumn("Amount"),
                new DataColumn("Balance"),
            });

            foreach (var t in transactions) 
            {
                dt.Rows.Add(
                    t.actID,
                    t.actType,
                    t.onDate,
                    t.description,
                    t.category,
                    t.transType,
                    t.amount,
                    t.balance
                    );
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Transactions.xlsx");
                }
            }
        }

        private bool TransactionExists(int id)
        {
            return _context.Transaction.Any(e => e.ID == id);
        }
    }
}
