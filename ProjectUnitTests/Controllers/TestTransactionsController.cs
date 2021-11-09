﻿using System;
using Xunit;
using CommerceBankProject.Controllers;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CommerceBankProject.Data;
using CommerceBankProject.Models;
using static ProjectUnitTests.Utilities.Utilities;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using CommerceBankProject.Areas.Identity.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ProjectUnitTests
{
    public class TestTransactionsController
    {
        TransactionsController controller;

        List<Transaction> transactions = new List<Transaction>
        {
            new Transaction()
            {
                ID = 789456123,
                customerID = "123789456",
                actID = "456123789",
                actType = "Customer",
                onDate = new DateTime(2008, 6, 1),
                balance = 1000.04m,
                transType = "Fun",
                description = "test desc",
                userEntered = false
            },
            new Transaction()
            {
                ID = 456789123,
                customerID = "111111111",
                actID = "222222222",
                actType = "Customer",
                onDate = new DateTime(2021, 7, 2),
                balance = 978.04m,
                transType = "Gas",
                description = "second description",
                userEntered = false
            }
        };

        //[Fact]
        //public async void TestIndex()
        //{
        //    List<Transaction> transactions = new List<Transaction>
        //    {
        //        new Transaction()
        //        {
        //            ID = 789456123,
        //            customerID = "123789456",
        //            actID = "456123789",
        //            actType = "Customer",
        //            onDate = new DateTime(2008, 6, 1),
        //            balance = 1000.04m,
        //            transType = "Fun",
        //            description = "test desc",
        //            userEntered = false
        //        },
        //        new Transaction()
        //        {
        //            ID = 456789123,
        //            customerID = "111111111",
        //            actID = "222222222",
        //            actType = "Customer",
        //            onDate = new DateTime(2021, 7, 2),
        //            balance = 978.04m,
        //            transType = "Gas",
        //            description = "second description",
        //            userEntered = false
        //        }
        //    };

        //    using (var context = new CommerceBankDbContext(TestDbContextOptions()))
        //    {
        //        foreach (Transaction t in transactions)
        //        {
        //            context.Transaction.Add(t);
        //        }

        //        ApplicationUser savedUser = new ApplicationUser();

        //        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        //            {new Claim(ClaimTypes.NameIdentifier, savedUser.Id)}, "TestAuthentication"));


        //        context.Users.Add(savedUser);
        //        context.SaveChanges();
        //        controller = new TransactionsController(context);

        //        controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        //        var result = await controller.Index();
        //        Assert.IsType<ViewResult>(result);
        //    }
        //    //var result = await controller.Index() as ViewResult;
        //    //Assert.IsType<ViewResult>(result);
        //}

        //[Fact]
        //public async void TestFilterIndex()
        //{
        //    using (var context = new CommerceBankDbContext(TestDbContextOptions()))
        //    {
        //        foreach (Transaction t in transactions)
        //        {
        //            context.Transaction.Add(t);
        //        }

        //        ApplicationUser savedUser = new ApplicationUser();

        //        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        //            {new Claim(ClaimTypes.NameIdentifier, savedUser.Id)}, "TestAuthentication"));

        //        context.Users.Add(savedUser);
        //        context.SaveChanges();

        //        controller = new TransactionsController(context);

        //        controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        //        var result = await controller.FilterIndex("all", "test", "2006-1-1", "2021-1-1", "20");
        //        Assert.IsType<ViewResult>(result);
        //    }
        //}

        [Fact]
        public async void TestGetDetailsView()
        {
            int? badID = null;
            int testID = 789456123;

            using (var context = new CommerceBankDbContext(TestDbContextOptions()))
            {
                Transaction testTransaction = new Transaction()
                {
                    ID = 789456123,
                    customerID = "111111111",
                    actID = "222222222",
                    actType = "Customer",
                    onDate = new DateTime(2021, 7, 2),
                    balance = 978.04m,
                    transType = "Gas",
                    description = "second description",
                    userEntered = false
                };

                context.Add(testTransaction);
                context.SaveChanges();

                controller = new TransactionsController(context);

                var notFoundResult = await controller.Details(badID);
                Assert.IsType<NotFoundResult>(notFoundResult);

                var result = await controller.Details(testID);
                var viewResult = result as ViewResult;

                Assert.IsType<ViewResult>(result);
                Assert.Equal(viewResult.Model, testTransaction);
            }
        }

        [Fact]
        public async void TestCreatePOSTWithValidModelState()
        {
            using (var context = new CommerceBankDbContext(TestDbContextOptions()))
            {
                Transaction testTransaction = new Transaction()
                {
                    ID = 789456123,
                    customerID = "111111111",
                    actID = "222222222",
                    actType = "Customer",
                    onDate = new DateTime(2021, 7, 2),
                    balance = 978.04m,
                    transType = "Gas",
                    description = "second description",
                    userEntered = false
                };

                controller = new TransactionsController(context);

                var result = await controller.Create(transaction: testTransaction);
                Assert.IsType<RedirectToActionResult>(result);

                var viewRes = result as RedirectToActionResult;
                Assert.Equal("Index", viewRes.ActionName);
            }
        }

        [Fact]
        public async void TestGetDeleteView()
        {
            int? badID = null;
            int testID = 789456123;

            using (var context = new CommerceBankDbContext(TestDbContextOptions()))
            {
                Transaction testTransaction = new Transaction()
                {
                    ID = 789456123,
                    customerID = "111111111",
                    actID = "222222222",
                    actType = "Customer",
                    onDate = new DateTime(2021, 7, 2),
                    balance = 978.04m,
                    transType = "Gas",
                    description = "second description",
                    userEntered = false
                };

                context.Add(testTransaction);
                context.SaveChanges();
                controller = new TransactionsController(context);

                var notFoundResult = await controller.Delete(badID);
                Assert.IsType<NotFoundResult>(notFoundResult);

                var result = await controller.Delete(testID);
                Assert.IsType<ViewResult>(result);

                var viewResult = result as ViewResult;
                Assert.Equal(viewResult.Model, testTransaction);
            }
        }

        [Fact]
        public async void TestDeleteConfirmed()
        {
            using (var context = new CommerceBankDbContext(TestDbContextOptions()))
            {
                Transaction testTransaction = new Transaction()
                {
                    ID = 789456123,
                    customerID = "111111111",
                    actID = "222222222",
                    actType = "Customer",
                    onDate = new DateTime(2021, 7, 2),
                    balance = 978.04m,
                    transType = "Gas",
                    description = "second description",
                    userEntered = false
                };

                context.Add(testTransaction);
                context.SaveChanges();

                Assert.Equal(testTransaction, context.Transaction.Find(789456123));

                controller = new TransactionsController(context);

                var result = await controller.DeleteConfirmed(789456123);
                Assert.IsType<RedirectToActionResult>(result);

                Assert.Null(context.Transaction.Find(789456123));

                var viewRes = result as RedirectToActionResult;
                Assert.Equal("Index", viewRes.ActionName);
            }
        }
    }
}