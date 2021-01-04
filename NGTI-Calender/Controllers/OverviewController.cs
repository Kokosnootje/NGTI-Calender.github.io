﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NGTI_Calender.Data;
using System.Net.Mail;
using NGTI_Calender.Models;

namespace NGTI_Calender.Controllers
{
    public class OverviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OverviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Overview/Index
        public IActionResult Index(string personId, string SelectedDate = "", string SelectedTimeslot = "", string AmountAvailablePlaces = "")
        {
            var amountRes = AmountReservedPlaces();
            List<Reservation> allRes = _context.Reservation.ToList();
            var testVar = sortAllRes(allRes);
            string[] selectedReservation = new string[] { SelectedDate, SelectedTimeslot };
            var tuple = Tuple.Create(_context.Timeslot.ToList(), Tuple.Create(SelectedDate, SelectedTimeslot, personId, AmountAvailablePlaces), testVar, _context.Person.ToList(), new Reservation(), Tuple.Create(amountRes, _context.Seats.ToList()[0].places, WhenBHV()), _context.Role.ToList());
            return View(tuple);
        }

        public List<Reservation> sortAllRes(List<Reservation> allTheRes)
        {
            List<Reservation> allTheSortedRes = new List<Reservation>();
            string[][] sortedRes = new string[allTheRes.Count][];
            int i = 0;
            foreach(var res in allTheRes)
            {
                sortedRes[i] = res.Date.Split('-');
                i++;
            }
            string[] sortedResTogetherNotDistinct = new string[allTheRes.Count];
            i = 0;
            foreach(string[] stringDates in sortedRes)
            {
                if(stringDates[1].Length < 2)
                {
                    stringDates[1] = "0" + stringDates[1];
                }
                if(stringDates[0].Length < 2)
                {
                    stringDates[0] = "0" + stringDates[0];
                }
                sortedResTogetherNotDistinct[i] = stringDates[2] + stringDates[1] + stringDates[0];
                i++;
            }
            string[] sortedResTogether = sortedResTogetherNotDistinct.Distinct().ToArray();
            sortedResTogether = sortedResTogether.OrderBy(x => x).ToArray();
            foreach(string sortedDate in sortedResTogether)
            {

                string Year = sortedDate[0].ToString() + sortedDate[1].ToString() + sortedDate[2].ToString() + sortedDate[3].ToString();
                string Month = "";
                string Day = "";
                if(sortedDate[4].ToString() == "0")
                {
                    Month = sortedDate[5].ToString();
                } else
                {
                    Month = sortedDate[4].ToString() + sortedDate[5].ToString();
                }
                if (sortedDate[6].ToString() == "0")
                {
                    Day = sortedDate[7].ToString();
                } else {
                    Day = sortedDate[6].ToString() + sortedDate[7].ToString();
                }
                string completedComparableDate = Day + "-" + Month + "-" + Year;
                foreach (Reservation res in allTheRes)
                {
                    if(completedComparableDate == res.Date)
                    {
                        allTheSortedRes.Add(res);
                    }
                }
            }
            return allTheSortedRes;
        }

        // SEND MAIL + RETURN VIEW
        [HttpPost]
        public async Task<IActionResult> Index(int personId, string subject, string body)
        {
            string email = "";
            foreach (var person in _context.Person.ToList())
            {
                if (person.PersonId == personId)
                {
                    email = person.EMail;
                }
            }
            // Server settings
            SmtpClient SmtpServer = new SmtpClient();
            SmtpServer.Port = 587;
            SmtpServer.Host = "smtp.gmail.com";
            SmtpServer.EnableSsl = true;
            SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
            SmtpServer.UseDefaultCredentials = false;
            SmtpServer.Credentials = new System.Net.NetworkCredential("mailcinemaconfirmation@gmail.com", "ProjectB");

            // Mail reciever and the body of the mail
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("mailcinemaconfirmation@gmail.com");
            mail.To.Add(new MailAddress(email));
            mail.Subject = subject;
            mail.Body = body;

            //Json bestand met films openen en lezenmail.Body = "Beste klant. Uw reservering is ontvangen en verwerkt. Laat deze mail zien in de bioscoop als toegangsbewijs. Geniet van de film!";
            SmtpServer.Send(mail);
            return View(_context.Timeslot.ToArray());
        }
        // POST: Overview/Index
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string resList, string personId)
        {
            string p = personId;
            string[] resids = resList.Split(' ');
            int[] reservationIds = new int[resids.Length];
            for (int j = 0; j < resids.Length; j++)
            {
                reservationIds[j] = Int32.Parse(resids[j]);
            }
            for (int i = 0; i < resids.Length; i++)
            {
                var reservation = await _context.Reservation.FindAsync(reservationIds[i]);
                _context.Reservation.Remove(reservation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { personId = personId });
        }

        // POST: Overview/GetAllReservations
        [HttpPost, ActionName("GetAllReservations")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllReservations(string selectedDate, string selectedTimeslot, string personId, string amountAvailablePlaces)
        {
            return RedirectToAction("Index", new { personId = personId, SelectedDate = selectedDate, SelectedTimeslot = selectedTimeslot, AmountAvailablePlaces = amountAvailablePlaces });
        }

        public async Task<IActionResult> RemoveReservation(string personId, string reservationId) {
            foreach (var res in _context.Reservation.ToList()) {
                if (res.ReservationId.ToString() == reservationId) {
                    _context.Reservation.Remove(res);
                    await _context.SaveChangesAsync();
                    break;
                }
            }
            return RedirectToAction("Index", new { personId = personId });
        }


        public int[][] AmountReservedPlaces()
        {
            //load upcoming 2 weeks - weekend
            DateTime[] days = new DateTime[10];
            DateTime lastDay = DateTime.Now;
            for (int i = 0; i < 10; i++)
            {
                if (lastDay.DayOfWeek == DayOfWeek.Saturday)
                {
                    lastDay = lastDay.AddDays(2.0);
                    days[i] = lastDay;
                    lastDay = lastDay.AddDays(1.0);
                }
                else if (lastDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    lastDay = lastDay.AddDays(1.0);
                    days[i] = lastDay;
                    lastDay = lastDay.AddDays(1.0);

                }
                else
                {
                    if (lastDay.DayOfWeek == DayOfWeek.Monday)
                    {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    }
                    if (lastDay.DayOfWeek == DayOfWeek.Tuesday)
                    {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    }
                    else if (lastDay.DayOfWeek == DayOfWeek.Wednesday)
                    {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    }
                    else if (lastDay.DayOfWeek == DayOfWeek.Thursday)
                    {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    }
                    else if (lastDay.DayOfWeek == DayOfWeek.Friday)
                    {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    }

                }
            }
            int[][] count = new int[10][];
            int indexI = 0;
            int indexJ = 0;
            foreach (var dt in days)
            {
                indexJ = 0;
                //create 2d array with amount of timeslots
                count[indexI] = new int[_context.Timeslot.ToArray().Length];
                foreach (var ts in _context.Timeslot.ToArray())
                {
                    foreach (var res in _context.Reservation.ToArray())
                        //check date overlap between day & res
                        if (res.Date == dt.Date.ToShortDateString())
                        {
                            //check timeslot overlap between ts & res
                            if (res.Timeslot.TimeslotId == ts.TimeslotId)
                            {
                                //if overlap add count
                                count[indexI][indexJ]++;
                            }
                        }
                    indexJ++;
                }
                indexI++;
            }
            return count;
        }

        protected int[][] WhenBHV() {

            //load upcoming 2 weeks - weekend
            DateTime[] days = new DateTime[10];
            DateTime lastDay = DateTime.Now;
            for (int i = 0; i < 10; i++) {
                if (lastDay.DayOfWeek == DayOfWeek.Saturday) {
                    lastDay = lastDay.AddDays(2.0);
                    days[i] = lastDay;
                    lastDay = lastDay.AddDays(1.0);
                } else if (lastDay.DayOfWeek == DayOfWeek.Sunday) {
                    lastDay = lastDay.AddDays(1.0);
                    days[i] = lastDay;
                    lastDay = lastDay.AddDays(1.0);

                } else {
                    if (lastDay.DayOfWeek == DayOfWeek.Monday) {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    }
                    if (lastDay.DayOfWeek == DayOfWeek.Tuesday) {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    } else if (lastDay.DayOfWeek == DayOfWeek.Wednesday) {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    } else if (lastDay.DayOfWeek == DayOfWeek.Thursday) {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    } else if (lastDay.DayOfWeek == DayOfWeek.Friday) {
                        days[i] = lastDay;
                        lastDay = lastDay.AddDays(1.0);
                    }

                }
            }
            int[][] arr = new int[10][];
            int indexI = 0;
            int indexJ = 0;
            foreach (var dt in days) {
                indexJ = 0;
                //create 2d array with amount of timeslots
                arr[indexI] = new int[_context.Timeslot.ToArray().Length];
                foreach (var ts in _context.Timeslot.ToArray()) {
                    foreach (var res in _context.Reservation.ToArray())
                        //check date overlap between day & res
                        if (res.Date == dt.Date.ToShortDateString()) {
                            //check timeslot overlap between ts & res
                            foreach(var person in _context.Person.ToList()) {
                                //match person + roles to the reservation
                                if(person.PersonId == res.PersonId) {
                                    foreach(var role in _context.Role.ToList()) {
                                        if (person.RolesId == role.RolesId) {
                                            if (role.BHV && res.Timeslot.TimeslotId == ts.TimeslotId) {
                                                //if person is a BHV add count
                                                arr[indexI][indexJ]++;

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    indexJ++;
                }
                indexI++;
            }
            return arr;
        }
    }
}
