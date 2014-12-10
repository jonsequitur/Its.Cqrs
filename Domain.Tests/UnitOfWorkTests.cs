using System;
using System.Collections.Generic;
using System.Data;
using System.Reactive.Disposables;
using FluentAssertions;
using Microsoft.Its.Recipes;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class UnitOfWorkTests
    {
        [SetUp]
        public void SetUp()
        {
            UnitOfWork<IDisposable>.ConfigureDefault();
            UnitOfWork<Bag>.ConfigureDefault();
            UnitOfWork<string>.ConfigureDefault();
        }

        [Test]
        public void Default_configuration_returns_UnitOfWork_having_null_Subject()
        {
            using (new UnitOfWork<IDisposable>())
            {
                UnitOfWork<IDisposable>.Current.Subject.Should().BeNull();
            }
        }

        [Test]
        public void Default_configuration_Commit_does_not_throw()
        {
            using (var work = new UnitOfWork<IDisposable>())
            {
                work.VoteCommit();
            }
        }

        [Test]
        public void Default_configuration_Reject_does_not_throw()
        {
            using (new UnitOfWork<IDisposable>())
            {
            }
        }

        [Test]
        public void Nested_unit_of_work_does_not_call_Commit_and_outer_unit_of_work_does()
        {
            var disposed = false;
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => disposed = true));

            using (var outer = new UnitOfWork<IDisposable>())
            {
                using (var inner = new UnitOfWork<IDisposable>())
                {
                    inner.VoteCommit();
                }
                Assert.False(disposed);

                outer.VoteCommit();
            }

            Assert.True(disposed);
        }

        [Test]
        public void When_nested_unit_of_work_does_not_commit_resource_then_outer_unit_of_work_closing_does_not_not_commit_it_either()
        {
            var committed = false;
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => { }));
            UnitOfWork<IDisposable>.Commit = disposable => { committed = true; };

            using (var outer = new UnitOfWork<IDisposable>())
            {
                using (new UnitOfWork<IDisposable>())
                {
                }
                outer.VoteCommit();
            }
            Assert.False(committed);
        }

        [Test]
        public void Nested_unit_of_work_does_not_recreate_resource_when_factory_is_specified_in_both_units_of_work()
        {
            var factoryCalls = 0;
            UnitOfWork<IDisposable>.Create = (_, setSubject) =>
            {
                factoryCalls++;
                setSubject(Disposable.Create(() => { }));
            };

            using (new UnitOfWork<IDisposable>())
            {
                Assert.That(factoryCalls, Is.EqualTo(1));
                using (new UnitOfWork<IDisposable>())
                {
                    Assert.That(factoryCalls, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void Nested_unit_of_work_does_not_recreate_resource_when_factory_is_specified_only_in_outer_units_of_work()
        {
            var factoryCalls = 0;
            UnitOfWork<IDisposable>.Create = (_, setSubject) =>
            {
                factoryCalls++;
                setSubject(Disposable.Create(() => { }));
            };

            using (new UnitOfWork<IDisposable>())
            {
                Assert.That(factoryCalls, Is.EqualTo(1));
                using (new UnitOfWork<IDisposable>())
                {
                    Assert.That(factoryCalls, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void When_UnitOfWork_is_disposed_more_than_once_it_does_not_recommit_work()
        {
            var commitCount = 0;
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => { }));
            UnitOfWork<IDisposable>.Commit = disposable => { commitCount++; };

            var work = new UnitOfWork<IDisposable>();
            work.VoteCommit();
            work.Dispose();
            work.Dispose();

            commitCount.Should().Be(1);
        }

        [Test]
        public void When_UnitOfWork_is_disposed_more_than_once_it_does_not_rereject_work()
        {
            var rejectCount = 0;
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => { }));
            UnitOfWork<IDisposable>.Reject = disposable => { rejectCount++; };

            var work = new UnitOfWork<IDisposable>();
            work.Dispose();
            work.Dispose();

            rejectCount.Should().Be(1);
        }

        [Test]
        public void Create_is_not_called_more_than_once_during_unit_of_work_nesting()
        {
            int callCount = 0;
            UnitOfWork<IDbConnection>.Create = (_, setSubject) =>
            {
                callCount++;
                setSubject(new Mock<IDbConnection>().Object);
            };

            using (new UnitOfWork<IDbConnection>())
            using (new UnitOfWork<IDbConnection>())
            using (new UnitOfWork<IDbConnection>())
            {
            }

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Commit_is_not_called_more_than_once_when_nested_units_of_work_exit()
        {
            int callCount = 0;
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Empty);
            UnitOfWork<IDisposable>.Commit = _ => { callCount++; };

            using (var one = new UnitOfWork<IDisposable>())
            {
                using (var two = new UnitOfWork<IDisposable>())
                {
                    using (var three = new UnitOfWork<IDisposable>())
                    {
                        one.VoteCommit();
                        two.VoteCommit();
                        three.VoteCommit();
                    }
                }
            }

            callCount.Should().Be(1);
        }

        [Test]
        public void Current_unit_of_work_returns_expected_instance_when_there_is_an_active_unit_of_work()
        {
            UnitOfWork<Bag>.Create = (_, setSubject) => setSubject(new Bag());
            using (var work = new UnitOfWork<Bag>())
            {
                Assert.That(UnitOfWork<Bag>.Current.Subject, Is.SameAs(work.Subject));
            }
        }

        [Test]
        public void Current_unit_of_work_returns_null_when_there_is_no_active_unit_of_work()
        {
            Assert.That(UnitOfWork<Bag>.Current, Is.Null);
        }

        [Test]
        public void When_UnitOfWork_is_committed_then_static_Committed_event_is_raised()
        {
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => { }));

            bool committedRaised = false;

            UnitOfWork<IDisposable>.Committed += (sender, disposable) => { committedRaised = true; };

            using (var work = new UnitOfWork<IDisposable>())
            {
                work.VoteCommit();
            }

            committedRaised.Should().BeTrue();
        }

        [Test]
        public void When_UnitOfWork_is_rejected_then_static_Committed_event_is_not_raised()
        {
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => { }));

            bool committedRaised = false;

            UnitOfWork<IDisposable>.Committed += (sender, disposable) => { committedRaised = true; };

            using (new UnitOfWork<IDisposable>())
            {
            }

            committedRaised.Should().BeFalse();
        }

        [Test]
        public void When_UnitOfWork_is_rejected_due_to_exception_then_Exception_is_set()
        {
            using (var one = new UnitOfWork<IDisposable>())
            {
                one.RejectDueTo(new Exception("nope."));

                one.Exception.Should().NotBeNull();
                one.Exception.Message.Should().Be("nope.");
            }
        }

        [Test]
        public void When_nested_UnitOfWork_is_rejected_due_to_exception_then_Exception_is_set_on_outer()
        {
            using (var one = new UnitOfWork<IDisposable>())
            {
                using (var two = new UnitOfWork<IDisposable>())
                {
                    using (var three = new UnitOfWork<IDisposable>())
                    {
                        three.RejectDueTo(new Exception("nope."));
                    }
                    two.VoteCommit();
                }

                one.Exception.Should().NotBeNull();
                one.Exception.Message.Should().Be("nope.");
            }
        }

        [Test]
        public void When_UnitOfWork_is_committed_then_static_Rejected_event_is_not_raised()
        {
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => { }));

            bool rejectedRaised = false;

            UnitOfWork<IDisposable>.Rejected += (sender, disposable) => { rejectedRaised = true; };

            using (var work = new UnitOfWork<IDisposable>())
            {
                work.VoteCommit();
            }

            rejectedRaised.Should().BeFalse();
        }

        [Test]
        public void When_UnitOfWork_is_rejected_then_static_Rejected_event_is_raised()
        {
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => { }));

            bool rejectedRaised = false;

            UnitOfWork<IDisposable>.Rejected += (sender, disposable) => { rejectedRaised = true; };

            using (var work = new UnitOfWork<IDisposable>())
            {
            }

            rejectedRaised.Should().BeTrue();
        }

        [Test]
        public void Create_can_be_used_to_specify_resources_available_to_the_unit_of_work_and_nested_units_of_work()
        {
            var obj = new object();
            UnitOfWork<string>.Create = (work, setSubject) =>
            {
                work.AddResource(obj);
                setSubject("hello");
            };

            using (var outer = new UnitOfWork<string>())
            {
                outer.Resource<object>().Should().BeSameAs(obj);
                using (var inner = new UnitOfWork<string>())
                {
                    inner.Resource<object>().Should().BeSameAs(obj);
                }
            }
        }

        [Test]
        public void Disposable_Subject_is_disposed_by_default()
        {
            var disposed = false;
            UnitOfWork<IDisposable>.Create = (_, setSubject) => setSubject(Disposable.Create(() => disposed = true));

            using (new UnitOfWork<IDisposable>())
            {
            }

            disposed.Should().BeTrue();
        }

        [Test]
        public void Disposable_resources_are_disposed_by_default()
        {
            var disposed = false;
            UnitOfWork<string>.Create = (work, setSubject) =>
            {
                work.AddResource(Disposable.Create(() => disposed = true));
                setSubject("hello");
            };

            using (new UnitOfWork<string>())
            {
            }

            disposed.Should().BeTrue();
        }

        [Test]
        public void Disposable_resources_can_be_flagged_to_not_be_disposed()
        {
            var disposed = false;
            UnitOfWork<string>.Create = (work, setSubject) =>
            {
                work.AddResource(Disposable.Create(() => disposed = true), dispose: false);
                setSubject("hello");
            };

            using (new UnitOfWork<string>())
            {
            }

            disposed.Should().BeFalse();
        }

        [Test]
        public void When_Commit_throws_then_unit_of_work_is_rejected()
        {
            var rejectCalled = false;
            UnitOfWork<string>.Create = (work, setSubject) => setSubject(Any.String());
            UnitOfWork<string>.Commit = work => { throw new Exception("BOOM!"); };
            UnitOfWork<string>.Reject = work => rejectCalled = true;

            using (var work = new UnitOfWork<string>())
            {
                work.VoteCommit();
            }

            rejectCalled.Should().BeTrue();
        }

        [Test]
        public void When_commit_and_reject_actions_are_specified_in_the_constructor_then_commit_is_only_called_when_the_outer_unit_of_work_is_committed()
        {
            var disposable = new BooleanDisposable();
            var rejectCount = 0;
            var commitCount = 0;
            Func<UnitOfWork<BooleanDisposable>> create = () =>
                                                         new UnitOfWork<BooleanDisposable>(() => disposable,
                                                                                           reject: d => rejectCount++,
                                                                                           commit: d => commitCount++);

            using (var outer = create())
            {
                using (var inner = create())
                {
                    inner.VoteCommit();
                }
                disposable.IsDisposed.Should().BeFalse();
                outer.VoteCommit();
            }

            commitCount.Should().Be(1);
            rejectCount.Should().Be(0);
            disposable.IsDisposed.Should().BeTrue();
        }

        [Test]
        public void When_commit_and_reject_actions_are_specified_in_the_constructor_then_reject_is_only_called_when_the_outer_unit_of_work_is_committed()
        {
            var disposable = new BooleanDisposable();
            var rejectCount = 0;
            var commitCount = 0;
            Func<UnitOfWork<BooleanDisposable>> create = () =>
                                                         new UnitOfWork<BooleanDisposable>(() => disposable,
                                                                                           reject: d => rejectCount++,
                                                                                           commit: d => commitCount++);

            using (var outer = create())
            {
                using (create())
                {
                }
                disposable.IsDisposed.Should().BeFalse();
                rejectCount.Should().Be(1);
                outer.VoteCommit();
            }

            commitCount.Should().Be(0);
            rejectCount.Should().Be(1);
            disposable.IsDisposed.Should().BeTrue();
        }

        private class Bag : HashSet<string>, IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}