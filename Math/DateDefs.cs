using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Originally from ConsultTodayDateDefs
/// </summary>
namespace MDTools.Math {

    public class DateDefs {
        private string _WeekStartDate;
        private DateTime _FirstQuarterStartDate;
        private DateTime _LastDateOfFourthQuarter;//The last date of a financial quarter division of the year

        #region Predefined Days/Holidays

        public DateTime ThanksGivingDay {
            get {
                return ThanksgivingDay();
            }
        }
        public DateTime BlackFriday {
            get {
                return tomorrow( ThanksgivingDay() );
            }
        }
        public DateTime WomenDay {
            get { return new DateTime( DateTime.Today.Year, 3, 8 ); }
        }
        public DateTime IndependanceDay {
            get { return new DateTime( DateTime.Today.Year, 7, 4 ); }
        }
        public DateTime NewYearsDay {
            get { return new DateTime( DateTime.Today.Year, 1, 1 ); }

        }
        public DateTime MartinLutherKingDay {
            get { return nextNthXday( new DateTime( DateTime.Today.Year, 1, 1 ), 3, "Monday" ); }
        }
        public DateTime WashingtonsBirthDay {
            get { return nextNthXday( new DateTime( DateTime.Today.Year, 2, 1 ), 3, "Monday" ); }
        }
        public DateTime MemorialDay//Last Monday in May
        {
            get {
                List<DateTime> l = new List<DateTime>();
                l = getXDaysFor( 5, DateTime.Today.Year, "Monday" );
                return l[l.Count - 1];
            }
        }
        public DateTime LaborDay {
            get { return nextNthXday( new DateTime( DateTime.Today.Year, 9, 1 ), 1, "Monday" ); }

        }
        public DateTime ColombusDay {
            get { return nextNthXday( new DateTime( DateTime.Today.Year, 10, 1 ), 2, "Monday" ); }

        }
        public DateTime XmasDay {
            get { return new DateTime( DateTime.Today.Year, 12, 25 ); }
        }
        public DateTime RemembranceDay {
            get { return new DateTime( DateTime.Today.Year, 11, 11 ); }
        }


        public DateTime ThanksGivingDayOf( int year ) {
            return nextNthXday( new DateTime( year, 11, 1 ), 4, "Thursday" );
        }
        public DateTime BlackFridayOf( int year ) {
            return tomorrow( ThanksGivingDayOf( year ) );

        }
        public DateTime WomenDayOf( int year ) {
            return new DateTime( year, 3, 8 );
        }
        public DateTime IndependanceDayOf( int Year ) {
            return new DateTime( Year, 7, 4 );
        }
        public DateTime NewYearsDayOf( int Year ) {
            return new DateTime( Year, 1, 1 );

        }
        public DateTime MartinLutherKingDayOf( int Year ) {
            return nextNthXday( new DateTime( Year, 1, 1 ), 3, "Monday" );
        }
        public DateTime WashingtonsBirthDayOf( int Year ) {
            return nextNthXday( new DateTime( Year, 2, 1 ), 3, "Monday" );
        }
        public DateTime MemorialDayOf( int Year )//Last Monday in May
        {
            List<DateTime> l = new List<DateTime>();
            l = getXDaysFor( 5, Year, "Monday" );
            return l[l.Count - 1];
        }
        public DateTime LaborDayOf( int Year ) {
            return nextNthXday( new DateTime( Year, 9, 1 ), 1, "Monday" );

        }
        public DateTime ColombusDayOf( int Year ) {
            return nextNthXday( new DateTime( Year, 10, 1 ), 2, "Monday" );
        }
        public DateTime XmasDayOf( int Year ) {
            return new DateTime( Year, 12, 25 );
        }
        public DateTime RemembranceDayOf( int Year ) {
            return new DateTime( Year, 11, 11 );
        }

        #endregion

        public string WeekStartDate {
            get { return _WeekStartDate; }
            set { _WeekStartDate = value; }
        }


        public DateTime FirstQuarterStartDate {
            get { return _FirstQuarterStartDate; }
            set {
                _FirstQuarterStartDate = value;
                _LastDateOfFourthQuarter =
                new DateTime( _FirstQuarterStartDate.Year + 1,
                             _FirstQuarterStartDate.Month,
                             _FirstQuarterStartDate.Day );
            }
        }

        public DateTime LastDateOfFourthQuarter {
            get { return _LastDateOfFourthQuarter; }
        }
        public List<DateTime> GetFirstQuarterDays() {
            List<DateTime> l = new List<DateTime>();
            DateTime d = FirstQuarterStartDate;
            for( ; d.CompareTo( LastDateOfFourthQuarter ) != 0; d = tomorrow( d ) ) {
                if( l.Count == 91 )
                    break;
                else
                    l.Add( d );
            }
            return l;
        }

        public List<DateTime> GetSecondQuarterDays() {
            List<DateTime> l = new List<DateTime>();
            l = GetFirstQuarterDays();
            DateTime d = l[l.Count - 1];
            l.Clear();
            for( ; d.CompareTo( LastDateOfFourthQuarter ) != 0; d = tomorrow( d ) ) {
                if( l.Count == 91 )
                    break;
                else
                    l.Add( d );
            }
            return l;
        }


        public List<DateTime> GetThirdQuarterDays() {
            List<DateTime> l = new List<DateTime>();
            l = GetSecondQuarterDays();
            DateTime d = l[l.Count - 1];
            l.Clear();
            for( ; d.CompareTo( LastDateOfFourthQuarter ) != 0; d = tomorrow( d ) ) {
                if( l.Count == 91 )
                    break;
                else
                    l.Add( d );
            }
            return l;
        }


        public List<DateTime> GetFourthQuarterDays() {
            List<DateTime> l = new List<DateTime>();
            l = GetThirdQuarterDays();
            DateTime d = l[l.Count - 1];
            l.Clear();
            for( ; d.CompareTo( LastDateOfFourthQuarter ) != 0; d = tomorrow( d ) )
                l.Add( d );
            return l;
        }

        public int QuarterTodayIsIn() {
            int Quarter = 0;
            if( GetFirstQuarterDays().Contains( DateTime.Today ) )
                Quarter = 1;
            if( GetSecondQuarterDays().Contains( DateTime.Today ) )
                Quarter = 2;
            if( GetThirdQuarterDays().Contains( DateTime.Today ) )
                Quarter = 3;
            if( GetFourthQuarterDays().Contains( DateTime.Today ) )
                Quarter = 4;
            return Quarter;
        }

        public int QuarterOfThisDate( DateTime d ) {
            int Quarter = 0;
            if( GetFirstQuarterDays().Contains( d ) )
                Quarter = 1;
            if( GetSecondQuarterDays().Contains( d ) )
                Quarter = 2;
            if( GetThirdQuarterDays().Contains( d ) )
                Quarter = 3;
            if( GetFourthQuarterDays().Contains( d ) )
                Quarter = 4;
            return Quarter;
        }
        /// <summary>
        /// This function returns the last day of the month
        /// </summary>
        /// <param name="d">Any day of the month </param>
        /// <returns></returns>
        public DateTime lastDay( DateTime d ) {

            //Jan, 
            if( d.Month == 1 || d.Month == 3 || d.Month == 5
                || d.Month == 7 || d.Month == 8 || d.Month == 10
                || d.Month == 12 )
                return new DateTime( d.Year, d.Month, 31 );
            if( d.Month == 2 && DateTime.IsLeapYear( d.Year ) )
                return new DateTime( d.Year, 2, 29 );

            if( d.Month == 2 && !DateTime.IsLeapYear( d.Year ) )
                return new DateTime( d.Year, 2, 28 );

            else
                return new DateTime( d.Year, d.Month, 30 );


        }

        private DateTime ThanksgivingDay() {
            return nextNthXday( new DateTime( DateTime.Today.Year, 10, 31 ), 4, "Thursday" );
        }
        public List<int> FindHolidays( List<DateTime> amongthesedays,
        List<DateTime> holidaylist ) {
            List<int> indices = new List<int>();
            for( int i = 0; i < amongthesedays.Count; i++ )
                foreach( DateTime d in holidaylist )
                    if( d.Equals( amongthesedays[i] ) )
                        indices.Add( i );

            return indices;
        }
        public static DateTime tomorrow( DateTime d ) {
            return d.AddDays( 1 );
        }
        public static DateTime yesterday( DateTime d ) {
            return d.AddDays( -1 );
        }
        public string BdayDOW( DateTime birthDay ) {
            return birthDay.DayOfWeek.ToString();
        }
        /// <summary>
        /// This method returns the next nth X day. Meaning
        /// If called like nextNthXday(DateTime.today,1,"Friday")
        /// then it will return the date of next Friday.
        /// </summary>
        /// <param name="start">Starting Date</param>
        /// <param name="n">How many</param>
        /// <param name="downame">Day name</param>
        /// <returns></returns>
        public static DateTime nextNthXday( DateTime start, int n, string downame ) {
            int counter = 0;
            for( ; ; start = tomorrow( start ) ) {
                if( start.DayOfWeek.ToString().IndexOf( downame ) != -1 )
                    counter++;
                if( counter == n )
                    break;
            }
            return start;
        }
        public static DateTime nextMonday( DateTime startdate ) { return nextNthXday( startdate, 1, "Monday" ); }
        public static DateTime nextTuesday( DateTime startdate ) { return nextNthXday( startdate, 1, "Tuesday" ); }
        public static DateTime nextWednesday( DateTime startdate ) { return nextNthXday( startdate, 1, "Wednesday" ); }
        public static DateTime nextThursday( DateTime startdate ) { return nextNthXday( startdate, 1, "Thursday" ); }
        public static DateTime nextFriday( DateTime startdate ) { return nextNthXday( startdate, 1, "Friday" ); }
        public static DateTime nextSaturday( DateTime startdate ) { return nextNthXday( startdate, 1, "Saturday" ); }
        public static DateTime nextSunday( DateTime startdate ) { return nextNthXday( startdate, 1, "Sunday" ); }

        //Last
        public static DateTime lastMonday( DateTime startdate ) { return prevNthXday( startdate, 1, "Monday" ); }
        public static DateTime lastTuesday( DateTime startdate ) { return prevNthXday( startdate, 1, "Tuesday" ); }
        public static DateTime lastWednesday( DateTime startdate ) { return prevNthXday( startdate, 1, "Wednesday" ); }
        public static DateTime lastThursday( DateTime startdate ) { return prevNthXday( startdate, 1, "Thursday" ); }
        public static DateTime lastFriday( DateTime startdate ) { return prevNthXday( startdate, 1, "Friday" ); }
        public static DateTime lastSaturday( DateTime startdate ) { return prevNthXday( startdate, 1, "Saturday" ); }
        public static DateTime lastSunday( DateTime startdate ) { return prevNthXday( startdate, 1, "Sunday" ); }

        public List<DateTime> getAllXDaysOf( int year, string downame ) {
            List<DateTime> l = new List<DateTime>();
            for( int i = 1; i < 13; i++ )
                l.AddRange( getXDaysFor( i, year, downame ) );
            return l;

        }
        public List<DateTime> getXDaysFor( int month, int year, string downame ) {
            List<DateTime> Xdays = new List<DateTime>();
            DateTime end;
            DateTime start = new DateTime( year, month, 1 );
            if( month + 1 <= 12 )
                end = new DateTime( year, month + 1, 1 );
            else
                end = new DateTime( year + 1, 1, 1 );
            for( ; ; start = tomorrow( start ) ) {
                if( start == end )
                    break;
                else {
                    if( start.DayOfWeek.ToString().Trim().ToLower().Equals( downame.Trim().ToLower() ) )
                        Xdays.Add( start );
                }
            }
            return Xdays;
        }
        public DateTime nextMonthNthXDay( int n, string downame ) {
            return nextNthXday( tomorrow( lastDay( DateTime.Today ) ), n, downame );
        }
        public static DateTime prevNthXday( DateTime start, int n, string downame ) {
            int counter = 0;
            for( ; ; start = yesterday( start ) ) {
                if( start.DayOfWeek.ToString().IndexOf( downame ) != -1 )
                    counter++;
                if( counter == n )
                    break;
            }
            return start;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="holidays"></param>
        /// <param name="weekendStyle"></param>
        /// <returns></returns>
        public List<DateTime> getWorkingDaysInRange( DateTime start, DateTime end,
            List<DateTime> holidays, int weekendStyle ) {
            //weekendStyle denotes how many days holidays on weenend. 1 or 2
            //1 if only sunday
            //2 if saturday and sunday
            //0 if none (Really bad 7 days a week!)
            List<DateTime> workingDays = new List<DateTime>();

            for( ; ; start = tomorrow( start ) ) {
                if( start > end )
                    break;
                if( !holidays.Contains( start ) ) {
                    if( weekendStyle == 0 )
                        workingDays.Add( start );
                    if( weekendStyle == 1 && !start.DayOfWeek.ToString().Equals( "Sunday" ) )
                        workingDays.Add( start );
                    if( weekendStyle == 2
                        && !start.DayOfWeek.ToString().Equals( "Saturday" )
                        && !start.DayOfWeek.ToString().Equals( "Sunday" ) )
                        workingDays.Add( start );
                }
            }
            return workingDays;
        }

        /// <summary>
        /// Returns the working days for a year
        /// </summary>
        /// <param name="year">For this year</param>
        /// <param name="holidays">List of official holidays</param>
        /// <param name="weekendStyle">1 if only Sunday is a holiday
        /// 2 if both saturday and sunday are holidays
        /// 0 if none is a holiday, i.e, 7 days working</param>
        /// <returns></returns>
        public List<DateTime> getWorkingDaysFor( int year, List<DateTime> holidays, int weekendStyle ) {

            List<DateTime> workingDays = new List<DateTime>();
            DateTime start = new DateTime( year, 1, 1 );
            DateTime end = new DateTime( year + 1, 1, 1 );
            for( ; ; start = tomorrow( start ) ) {
                if( start == end )
                    break;
                if( !holidays.Contains( start ) ) {
                    if( weekendStyle == 0 )
                        workingDays.Add( start );
                    if( weekendStyle == 1 && !start.DayOfWeek.ToString().Equals( "Sunday" ) )
                        workingDays.Add( start );
                    if( weekendStyle == 2
                        && !start.DayOfWeek.ToString().Equals( "Saturday" )
                        && !start.DayOfWeek.ToString().Equals( "Sunday" ) )
                        workingDays.Add( start );
                }
            }
            return workingDays;
        }

        /// <summary>
        /// Returns the last X day of the year
        /// </summary>
        /// <param name="year">For this year</param>
        /// <param name="x">of this day</param>
        /// <returns>DateTime</returns>
        /// <example>DateTime d = getLastXdayOf(2010,"Sunday");
        /// will return the last Sunday of 2010</example>
        public DateTime getLastXdayOf( int year, string x ) {
            List<DateTime> ds = getXDaysFor( 12, year, x );
            return ds[ds.Count - 1];

        }

        /// <summary>
        /// Returns the first X day of the year
        /// </summary>
        /// <param name="year"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public DateTime getFirstXdayOf( int year, string x ) {
            return nextNthXday( new DateTime( year, 1, 1 ), 1, x );
        }

        public DateTime getNthWeek( int N, string x ) {
            WeekStartDate = x;
            return nextNthXday( new DateTime( DateTime.Today.Year, 1, 1 ), N, WeekStartDate );
        }
        public int getWeekEndCount( int Year, string x ) {
            List<DateTime> l = new List<DateTime>();
            WeekStartDate = x;
            l = getAllXDaysOf( Year, WeekStartDate );
            return l.Count;
        }
    }
}
