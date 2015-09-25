namespace Flake.Tests
{
  using System;
  using System.Collections.Generic;
  using Shouldly;

  [TestFixture]
  public class IdWorkerFixture
  {
    private const long WorkerMask = 0x000000000001F000L;
    private const long DatacenterMask = 0x00000000003E0000L;
    private const ulong TimestampMask = 0xFFFFFFFFFFC00000UL;

    [Test]
    public void It_should_generate_an_id()
    {
      var worker = new IdWorker(1, 1);
      var v = worker.NextId();
      v.ShouldBeGreaterThan(0);
    }

    [Test]
    public void It_should_return_an_accurate_timestamp()
    {

    }

    [Test]
    public void It_should_return_the_correct_job_id()
    {
      var s = new IdWorker(1, 1);
      s.WorkerId.ShouldBe(1);
    }


    [Test]
    public void It_should_return_the_datacenter_id()
    {
      var s = new IdWorker(1, 1);
      s.DatacenterId.ShouldBe(1);
    }

    [Test]
    public void It_should_properly_mask_worker_id()
    {
      const long workerId = 0x1F;
      const int datacenterId = 0;
      var worker = new IdWorker(workerId, datacenterId);
      for (var i = 0; i < 1000; i++)
      {
        var id = worker.NextId();
        var expected = (id & WorkerMask) >> 12;
        workerId.ShouldBe(expected);
      }
    }

    [Test]
    public void It_should_properly_mask_the_datacenter_id()
    {
      const int workerId = 0x1F;
      const long datacenterId = 0;
      var worker = new IdWorker(workerId, datacenterId);
      for (var i = 0; i < 1000; i++)
      {
        var id = worker.NextId();
        var expected = (id & DatacenterMask) >> 17;
        datacenterId.ShouldBe(expected);
      }
    }

    [Test]
    public void It_should_properly_mask_timestamp()
    {
      var worker = new IdWorker(31, 31);
      for (var i = 0; i < 100; i++)
      {
        var t = TimeExtenions.CurrentTimeMillis();
        using (TimeExtenions.StubCurrentTime(t))
        {
          var id = worker.NextId();
          var actual = (((ulong)id & TimestampMask) >> 22);
          var expected = (t - IdWorker.Twepoch);
          actual.ShouldBe((ulong)expected);
        }
      }
    }

    [Test]
    public void It_should_roll_over_sequence_id()
    {
      // put a zero in the low bit so we can detect overflow from the sequence
      const long workerId = 4;
      const int datacenterId = 4;
      var worker = new IdWorker(workerId, datacenterId);
      const int startSequence = 0xFFFFFF - 20;
      const int endSequence = 0xFFFFFF + 20;
      worker.Sequence = startSequence;

      for (var i = startSequence; i <= endSequence; i++)
      {
        var id = worker.NextId();
        var actual = (id & WorkerMask) >> 12;
        actual.ShouldBe(workerId);
      }
    }

    [Test]
    public void It_should_generate_increasing_ids()
    {
      var worker = new IdWorker(1, 1);
      var lastId = 0L;
      for (var i = 0; i < 100; i++)
      {
        var id = worker.NextId();
        id.ShouldBeGreaterThan(lastId);
        lastId = id;
      }
    }

    [Test]
    public void It_should_generate_1_million_ids_quickly()
    {
      var worker = new IdWorker(31, 31);
      var t = TimeExtenions.CurrentTimeMillis();
      for (int i = 0; i < 1000000; i++)
      {
        var id = worker.NextId();
      }
      var t2 = TimeExtenions.CurrentTimeMillis();
      Console.WriteLine("generated 1000000 ids in {0} ms, or {1} ids/second", t2 - t, 1000000000.0 / (t2 - t));
    }

    [Test]
    public void It_should_sleep_if_we_rollover_twice_in_the_same_millisecond()
    {
      var worker = new WakingIdWorker(1, 1);
      var iter = new List<long>()
                           {
                               2, 2, 3
                           };
      int idx = 0;
      Func<long> timeFunc = () =>
      {
        var res = iter[idx++];
        if (idx > iter.Count - 1) idx = 0;
        return res;
      };

      using (TimeExtenions.StubCurrentTime(timeFunc))
      {
        worker.Sequence = 4095;
        worker.NextId();
        worker.Sequence = 4095;
        worker.NextId();
      }
      worker.Slept.ShouldBe(1);
    }

    [Test]
    public void It_should_generate_only_unique_ids()
    {
      var worker = new IdWorker(31, 31);
      var set = new HashSet<long>();
      const int N = 2000000;
      for (var i = 0; i < N; i++)
      {
        var id = worker.NextId();
        if (set.Contains(id))
        {
          Console.WriteLine("Found duplicate : {0}", id);
        }
        else
        {
          set.Add(id);
        }
      }
      set.Count.ShouldBe(N);
    }

    [Test]
    public void It_should_generate_ids_over_50_billion()
    {
      var worker = new IdWorker(0, 0);
      var id = worker.NextId();
      id.ShouldBeGreaterThan(50000000000L);
    }

    [Test]
    public void It_should_generate_only_unique_ids_even_when_time_goes_backward()
    {
      const long sequenceMask = -1L ^ (-1L << 12);
      var worker = new StaticTimeWorker(0, 0);

      // reported at https://github.com/twitter/snowflake/issues/6
      // first we generate 2 ids with the same time, so that we get the sequqence to 1
      worker.Sequence.ShouldBe(0);
      worker.Time.ShouldBe(1);
      var id1 = worker.NextId();

      (id1 >> 22).ShouldBe(1);
      (id1 & sequenceMask).ShouldBe(0);

      worker.Sequence.ShouldBe(0);
      worker.Time.ShouldBe(1);
      var id2 = worker.NextId();

      (id2 >> 22).ShouldBe(1);
      (id2 & sequenceMask).ShouldBe(1);

      //then we set the time backwards

      worker.Time = 0;
      worker.Sequence.ShouldBe(1);
      Should.Throw<InvalidSystemClockException>(() => worker.NextId());
      worker.Sequence.ShouldBe(1); // this used to get reset to 0, which would cause conflicts

      worker.Time = 1;
      var id3 = worker.NextId();

      (id3 >> 22).ShouldBe(1);
      (id3 & sequenceMask).ShouldBe(2);
    }
  }
}
