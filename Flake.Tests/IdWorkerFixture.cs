namespace Flake.Tests
{
    using System;
    using System.Collections.Generic;

    public class IdWorkerFixture
    {

        private const long WorkerMask = 0x000000000001F000L;
        private const long DatacenterMask = 0x00000000003E0000L;
        private const ulong TimestampMask = 0xFFFFFFFFFFC00000UL;

        [Fact]
        public void It_should_generate_an_id()
        {
            var worker = new IdWorker(1, 1);
            var v = worker.NextId();
            v.Should().BeGreaterThan(0);
        }

        [Fact]
        public void It_should_return_an_accurate_timestamp()
        {
        }

        [Fact]
        public void It_should_return_the_correct_job_id()
        {
            var s = new IdWorker(1, 1);
            s.WorkerId.Should().Be(1);
        }

        [Fact]
        public void It_should_return_the_datacenter_id()
        {
            var s = new IdWorker(1, 1);
            s.DatacenterId.Should().Be(1);
        }

        [Fact]
        public void It_should_properly_mask_worker_id()
        {
            const long workerId = 0x1F;
            const int datacenterId = 0;
            var worker = new IdWorker(workerId, datacenterId);
            for (var i = 0; i < 1000; i++)
            {
                var id = worker.NextId();
                var expected = (id & WorkerMask) >> 12;
                workerId.Should().Be(expected);
            }
        }

        [Fact]
        public void It_should_properly_mask_the_datacenter_id()
        {
            const int workerId = 0x1F;
            const long datacenterId = 0;
            var worker = new IdWorker(workerId, datacenterId);
            for (var i = 0; i < 1000; i++)
            {
                var id = worker.NextId();
                var expected = (id & DatacenterMask) >> 17;
                datacenterId.Should().Be(expected);
            }
        }

        [Fact]
        public void It_should_properly_mask_timestamp()
        {
            var worker = new IdWorker(31, 31);
            for (var i = 0; i < 100; i++)
            {
                var t = TimeExtensions.CurrentTimeMillis();
                using (TimeExtensions.StubCurrentTime(t))
                {
                    var id = worker.NextId();
                    var actual = (((ulong) id & TimestampMask) >> 22);
                    var expected = (t - IdWorker.Twepoch);
                    actual.Should().Be((ulong) expected);
                }
            }
        }

        [Fact]
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
                actual.Should().Be(workerId);
            }
        }

        [Fact]
        public void It_should_generate_increasing_ids()
        {
            var worker = new IdWorker(1, 1);
            var lastId = 0L;
            for (var i = 0; i < 100; i++)
            {
                var id = worker.NextId();
                id.Should().BeGreaterThan(lastId);
                lastId = id;
            }
        }

        [Fact]
        public void It_should_generate_1_million_ids_quickly()
        {
            var worker = new IdWorker(31, 31);
            var t = TimeExtensions.CurrentTimeMillis();
            for (int i = 0; i < 1000000; i++)
            {
                var id = worker.NextId();
            }
            var t2 = TimeExtensions.CurrentTimeMillis();

            Console.WriteLine($"generated 1000000 ids in {t2 - t} ms, or {(1000000000.0 / (t2 - t))} ids/second");
        }

        [Fact]
        public void It_should_sleep_if_we_rollover_twice_in_the_same_millisecond()
        {
            var worker = new WakingIdWorker(1, 1);
            var iter = new List<long>
            {
                2,
                2,
                3
            };
            int idx = 0;

            long TimeFunc()
            {
                var res = iter[idx++];
                if (idx > iter.Count - 1)
                {
                    idx = 0;
                }

                return res;
            }

            using (TimeExtensions.StubCurrentTime((Func<long>) TimeFunc))
            {
                worker.Sequence = 4095;
                worker.NextId();
                worker.Sequence = 4095;
                worker.NextId();
            }
            worker.Slept.Should().Be(1);
        }

        [Fact]
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
                    Console.WriteLine($"Found duplicate : {id}");
                }
                else
                {
                    set.Add(id);
                }
            }
            set.Count.Should().Be(N);
        }

        [Fact]
        public void It_should_generate_ids_over_50_billion()
        {
            var worker = new IdWorker(0, 0);
            var id = worker.NextId();
            id.Should().BeGreaterThan(50000000000L);
        }

        [Fact]
        public void It_should_generate_only_unique_ids_even_when_time_goes_backward()
        {
            const long sequenceMask = -1L ^ (-1L << 12);
            var worker = new StaticTimeWorker(0, 0);

            // reported at https://github.com/twitter/snowflake/issues/6
            // first we generate 2 ids with the same time, so that we get the sequence to 1
            worker.Sequence.Should().Be(0);
            worker.Time.Should().Be(1);
            var id1 = worker.NextId();

            (id1 >> 22).Should().Be(1);
            (id1 & sequenceMask).Should().Be(0);

            worker.Sequence.Should().Be(0);
            worker.Time.Should().Be(1);
            var id2 = worker.NextId();

            (id2 >> 22).Should().Be(1);
            (id2 & sequenceMask).Should().Be(1);

            //then we set the time backwards

            worker.Time = 0;
            worker.Sequence.Should().Be(1);


            Action act = () => worker.NextId();
            act.Should().Throw<InvalidSystemClockException>();

            worker.Sequence.Should().Be(1); // this used to get reset to 0, which would cause conflicts

            worker.Time = 1;
            var id3 = worker.NextId();

            (id3 >> 22).Should().Be(1);
            (id3 & sequenceMask).Should().Be(2);
        }
    }
}