Спор между PashaPash и Malobukv об использовании IDisposable на форумах MSDN (закрыты в 2024 г.)

## Проверьте, пожалуйста, работу примера.

Источник: https://yandexwebcache.net/yandbtm?fmode=inject&tm=1712642003&tld=ru&lang=ru&la=1685160832&text=форумы+msdn+pashapash+malobukv+site%3Amicrosoft.com&url=https%3A//social.msdn.microsoft.com/Forums/ru-RU/2001ecc6-d241-439d-8b98-111b1eb67836/-%3Fforum%3Dvsru&l10n=ru&mime=html&sign=6f9e191b935c145ef9ed7437ccc3885a&keyno=0

Среда Visual Studio и языки программирования \> Работа в среде Visual Studio

Общие обсуждения

пожалуйста, проверьте что он выдает. код под WinForms. надо проверить на разных системах.
после запуска откроется окно. несколько раз нажмите кнопки: "create -> dispose" и "create -> clear -> dispose"
вывод направляется в "Output window" или "Immediate Windows"
опубликуйте его в ответ на это сообщение.

заранее спасибо.

```
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication12
{
    public partial class Form1 : Form
    {
        List<int[]> arrs = new List<int[]>();
        void LoadArray()
        {
            var arr = new int[100000];
            arrs.Add(arr);
        }
        public Form1()
        {
            var count = 500.0;
            Button b1 = new Button() { Parent = this, Text = "create -> dispose", Location = new Point(10, 10), Width = 200 };
            b1.Click += (s, e) =>
            {
                var sw = Stopwatch.StartNew();
                arrs.Clear();
                for (var i = 0; i < count; i++)
                {
                    using (var c = new Collection(0)) { }
                    LoadArray();
                }
                System.Diagnostics.Trace.WriteLine(sw.ElapsedMilliseconds, b1.Text);
            };
            Button b2 = new Button() { Parent = this, Text = "create -> clear -> dispose", Location = new Point(10, b1.Bottom + 5), Width = 200 };
            b2.Click += (s, e) => // с Clear заметно быстрее
            {
                var sw = Stopwatch.StartNew();
                arrs.Clear();
                for (var i = 0; i < count; i++)
                {
                    using (var c = new Collection(0))
                    {
                        c.Clear();
                    }
                    LoadArray();
                }
                System.Diagnostics.Trace.WriteLine(sw.ElapsedMilliseconds, b2.Text);
            };
        }
        class Entity : IDisposable
        {
            public int Id { get; private set; }
            public Entity(int id) { this.Id = id; }
            ~Entity() { }
            public virtual void Dispose() { GC.SuppressFinalize(this); }
        }
        class Collection : Entity
        {
            public IEnumerable<Entity> Items { get; private set; }
            public Collection(int id)
                : base(id)
            {
                var list = new List<Entity>();
                for (int i = 0; i < 10000; i++) list.Add(new Entity(i));
                this.Items = list;
            }
            public void Clear()
            {
                foreach (var itm in this.Items) itm.Dispose();
                ((List<Entity>)this.Items).Clear();
            }
            public override void Dispose()
            {
                this.Items = null;
                base.Dispose();
            }
        }
    }
}
```

P.S.
есть в коде баг или нет в коде бага обсуждаем в теме
http://social.msdn.microsoft.com/Forums/ru-RU/programminglanguageru/thread/92b54cb5-b3b4-47aa-a70d-af5ceb261f67

Изменено Malobukv 28 августа 2011 г. 16:01 добавил P.S.  
Изменен тип Abolmasov Dmitry 5 сентября 2011 г. 10:25  
23 августа 2011 г. 13:24

Рад переводу спора в конструктивное русло.

```
create -> dispose: 21594
create -> clear -> dispose: 27526
create -> dispose: 21875
create -> clear -> dispose: 26848
create -> dispose: 21411
create -> clear -> dispose: 26931
```

23 августа 2011 г. 13:34

Модератор

Win7 Enterprise 64 Bit

```
create -> dispose: 1752

create -> clear -> dispose: 729

create -> dispose: 1689

create -> clear -> dispose: 731

create -> dispose: 1741

create -> clear -> dispose: 775

create -> dispose: 1808

create -> clear -> dispose: 711

create -> dispose: 1712

create -> clear -> dispose: 738

create -> dispose: 1664

The thread '<No Name>' (0x2f84) has exited with code 0 (0x0).

create -> clear -> dispose: 815

create -> dispose: 1767

create -> clear -> dispose: 712

create -> dispose: 1790

create -> clear -> dispose: 726

create -> dispose: 1753

create -> clear -> dispose: 757

create -> dispose: 1838

create -> clear -> dispose: 746

create -> dispose: 1799

create -> clear -> dispose: 712

create -> dispose: 1718

create -> clear -> dispose: 750

create -> dispose: 1717

create -> clear -> dispose: 737

The thread '<No Name>' (0x1e34) has exited with code 0 (0x0).
```

23 августа 2011 г. 14:23

Немного странный вопрос - а в чем идея кода? Показать что неправильная реализация Dispose/Finalize приводит к проблемам производительности?

Очевидно же, что в первом куске кода все Entity остаются висеть в очереди на финализацию даже после вызова Dispose у Collection. Во втором - для них явно вызывается SuppressFinalize, потому и работает "быстрее". Это обычный баг. Если сделать нормальную реализацию Dispose/Finalize, то магический эффект от Clear исчезнет.

```
 class Entity : IDisposable
 {
 public int Id { get; private set; }
 public Entity(int id) { this.Id = id; }
 ~Entity()
 {
 this.Dispose(false);
 }
 private bool disposed = false;

 //Implement IDisposable.
 public void Dispose()
 {
 Dispose(true);
 GC.SuppressFinalize(this);
 }

 protected virtual void Dispose(bool disposing)
 {
 if (!disposed)
 {
  if (disposing)
  {
  // Free other state (managed objects).
  }
  // Free your own state (unmanaged objects).
  // Set large fields to null.
  disposed = true;
 }
 }
 }
 class Collection : Entity
 {
 public IEnumerable<Entity> Items { get; private set; }
 public Collection(int id)
 : base(id)
 {
 var list = new List<Entity>();
 for (int i = 0; i < 10000; i++) list.Add(new Entity(i));
 this.Items = list;
 }

 public void Clear()
 {
 foreach (var itm in this.Items) 
  itm.Dispose();
 ((List<Entity>)this.Items).Clear();
 }

 private bool disposed = false;

 protected override void Dispose(bool disposing)
 {
 if (!disposed)
 {
  if (disposing)
  {
  this.Clear(); // или только foreach из Clear тут, почти не влияет на результаты.
  }

  disposed = true;
 }

 base.Dispose(disposing);
 }
 }
```

Результаты примерно 600 - 800ms на моей машине, в релизе x86 не под отладкой. И?

UPD: Во избежание повторного удаления, я процитирую Илью Сазонова:

to PashaPash  в любой теме вы общаетесь только с одним человеком - автором вопроса. Вы не имеете права осуществлять цензуру чужих мнений, высказываний и кода (если они не нарушают грубо правила форума и закон) - если кто-то сделал ошибку, то вы можете привести автору темы контр-пример и дать ссылки на первоисточники от Microsoft (документацию MSDN, блоги разрабочиков и т.п.) без опровержений и перехода на личности.

Я указал вам на ошибку, привел контр-пример, и дал ссылку на документацию. Во избежание неверного вывода из результатов замеров - что clear, якобы, вляет на сбор мусора. Только попробуйте еще раз удалить этот пост с позорной припиской "тема с просьбой проверить работу кода, а не исправлять и цитировать книги". Надеюсь, остальные посетители оценят количество постов, удаленных вами "ради поддержания чистоты". 

Изменено PashaPash Moderator 24 августа 2011 г. 21:21 добавил явное обоснование воскрешения поста

23 августа 2011 г. 14:46

Модератор

для чего в Collection повторять код Entity? :)
может еще Sleep добавите, чтобы было так как вам надо? :)

Изменено PashaPashModerator 23 августа 2011 г. 16:50 убрал коммент с Clear  
Изменено Malobukv 23 августа 2011 г. 17:31 убрал цитирование. мешает. PashaPash не правьте мои сообщения.  
23 августа 2011 г. 15:29

для чего в Collection повторять код Entity? :)
может еще Sleep добавите, чтобы было так как вам надо? :)

P.S.
сорри за длинное цитирование.

Вы шутите или как? Это стандартная реализация IDisposable для базового класса и его наследника. Collection у вас IDisposable? Тогда вы должны реализовать этот интерфейс правильно:

Реализация методов Finalize и Dispose для очистки неуправляемых ресурсов

Откройте топик, просмотрите, сравните с моим кодом и со своим. Вы пробовали запустить мой код? Попробуйте, результат вас удивит - работает быстро вне зависимости от наличия Clear. Точнее, оба примера работают с тоже же скоростью, что пример с Clear в вашем варианте. Clear не нужен. :)

Конктретное место, которое у вас тормозит:

http://msdn.microsoft.com/ru-ru/library/system.idisposable.dispose.aspx

Цитата: При реализации этого метода необходимо убедиться, что все занятые ресурсы высвобождены путем передачи вызова по иерархии вложений.

Вы не вызываете Dispose вниз по иерархии из Collection в Entity. Из-за этого для отдельных Entity в вашем примере не вызывается SupressFinalize. Из-за этого тысячи Entity остаются висеть в очереди финализации.

А в куске с Clear вы внезапно решаете вызывать Dispose у отдельных Entity, что убирает их очереди на финализацию, и все работает быстрее.

Самое забавное в этом споре что ошибка в вашей Disposе очевидна каждому, кто читал хотя бы Рихтера. Это же базовый паттерн, его спрашивают на каждом собеседовании. И вне зависимости от количества дурацких шуток про Sleep ваш вопрос "для чего повторять код" звучит просто глупо для разработчика с хоть каким-то опытом. Для того чтобы не тормозило, не забивалась очередь финализации, чтобы не рождались мифы про clear, и сборщик мусора. Чтобы не выглядеть незнающим азов .net-а, в конце концов.

23 августа 2011 г. 15:49

Модератор

короче, из всего что вы написали, делаем вывод, что вы настаиваете на том, что в Collection.Dispose надо повторить код Entity.Dispose. рихтера вспомнили и что читать любите это хорошо.

возьмите reflector или ilspy и смотрите:

есть System.ComponentModel.Component - реализует IDisposable
в примере - это Entity
смотрим дальше. на System.Timers.Timer - наследует Component.
в примере это Collection
как видите в наследнике нет никаких проверок.

так что отвлекитесь от книг и заглинете в .NET
ниже мой пример, котовый вы подправили, и с моими комментами к правкам.

```
public partial class Form1 : Form
{
    List<int[]> arrs = new List<int[]>();
    void LoadArray()
    {
        var arr = new int[100000];
        arrs.Add(arr);
    }
    public Form1()
    {
        var count = 500.0;
        Button b1 = new Button() { Parent = this, Text = "create -> dispose", Location = new Point(10, 10), Width = 200 };
        b1.Click += (s, e) =>
        {
            var sw = Stopwatch.StartNew();
            arrs.Clear();
            for (var i = 0; i < count; i++)
            {
                using (var c = new Collection(0)) { }
                LoadArray();
            }
            System.Diagnostics.Trace.WriteLine(sw.ElapsedMilliseconds, b1.Text);
        };
        Button b2 = new Button() { Parent = this, Text = "create -> clear -> dispose", Location = new Point(10, b1.Bottom + 5), Width = 200 };
        b2.Click += (s, e) => // с Clear заметно быстрее
        {
            var sw = Stopwatch.StartNew();
            arrs.Clear();
            for (var i = 0; i < count; i++)
            {
                using (var c = new Collection(0))
                {
                    c.Clear();
                }
                LoadArray();
            }
            System.Diagnostics.Trace.WriteLine(sw.ElapsedMilliseconds, b2.Text);
        };
    }

    // ниже мой код с исправлениями "PashaPash Logic Software, Inc MCC, Partner
    // и моими коментами

    class Entity : IDisposable
    {
        public int Id { get; private set; }
        public Entity(int id) { this.Id = id; }
        ~Entity()
        {
            this.Dispose(false);
        }
        /*
            PashaPash написал:
            private bool disposed = false;
       
            COMMENT: для чего хранить состояние, если значение нигде не используется?
            ответ знает только PashaPash :)
        */
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            /*
            PashaPash написал:
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                }
                // Free your own state (unmanaged objects).
                // Set large fields to null.
                disposed = true;
            }
           
            COMMENT: для чего все эти пустые проверки и сохранение disposed?
            ответ знает только PashaPash :)
            */
        }
    }
    class Collection : Entity
    {
        public IEnumerable<Entity> Items { get; private set; }
        public Collection(int id)
            : base(id)
        {
            var list = new List<Entity>();
            for (int i = 0; i < 10000; i++) list.Add(new Entity(i));
            this.Items = list;
        }
        public void Clear()
        {
            foreach (var itm in this.Items)
                itm.Dispose();
            /*
                PashaPash закоментил строку
                ((List<Entity>)this.Items).Clear();
           
                COMMENT: PashaPash думал что никто не заметит?! :)
                возвращаем строку.
            */
            ((List<Entity>)this.Items).Clear();
        }
        /*
            PashaPash написал:
            private bool disposed = false;
       
            COMMENT: загляните в реализацию System.ComponentModel.Component
            и его наследника, например, System.Timers.Timer
            disposed есть в Component, но не в Timer. потому что оно здесь не нужно.
        */
        protected override void Dispose(bool disposing)
        {
            /*
            PashaPash написал:
            if (!disposed)
            {
                if (disposing)
                {
                    this.Clear(); // или только foreach из Clear тут, почти не влияет на результаты.
                }
                disposed = true;
            }
            base.Dispose(disposing);
           
            COMMENT: опять же заглянем в код .NET, например, в System.Data.Common.DataAdapter.Dispose
            также как и в моем коде обнуляется ссылка на коллекцию
            наверное PashaPash думал, что никто не заметит, что он повторно/незаметно вызывает Clear()? :)
            поэтому возвращаем как было.
            */
            if(disposing)     // вообще-то проверка лишняя. но пусть остается.
                this.Items = null;
            base.Dispose(disposing);
        }
    }
}
```

23 августа 2011 г. 16:44

Да, Clear случайно заккоментил уже после замеров. Сейчас исправлю, на скорость это не влияет.

Давайте уж начнем с вопроса: почему вы реализуете IDisposable для классов без неуправляемых ресурсов. Если вы ответите на этот вопрос - то поймете почему у меня в коде пустые проверки. А пока вы только все больше и больше показываете свое незнание основ .NET.

Вы тоже рихтера почитайте, говорят, помогает в работе. А то все по старинке, с рефлектором.

Хорошо то как - вы спорите, пытаетесь доказать что я что-то сделал криво. Но по факту у вас тормозит. У меня - нет. Я знаю как реализовать IDisposable/Finalize. Вы - явно нет. Я знаю зачем там пустые проверки, как и любой, работавший с неуправляемыми ресурсами. Вы - искрене им удивляетесь. Забавно, правда?

23 августа 2011 г. 16:49

Модератор

> Да, Clear случайно заккоментил уже после замеров

ага. чисто случайно из dispose вызвали Clear и т.д. :)

> почему вы реализуете IDisposable для классов без неуправляемых ресурсов

вы вообще в код .NET заглядывали? или по книжкам понахватались?
у вас есть Reflector или ILSpy? нет. так посмотрите

23 августа 2011 г. 16:59

Нет, я закомментил Clear в Clear потому что это было проще, чем копипастить foreach в диспоуз, для проверки утверждения "или только foreach из Clear тут, почти не влияет на результаты." Замеры проводил и с Clear, и без. На скорость не влияет.

В Collection переопределен Dispose только потому, что именно в Collection есть IDisposable поле, а не только наследование. При вызове Dispose вы должны вызвать Dispose не только вверх по иерархии:

Вот, из MSDN опять же:

Метод Dispose должен освобождать все ресурсы, удерживаемые данным объектом и любым объектом, которым он владеет.

У вас коллекция владетет набором entity. Хотите циатату из иcходников? Да как два пальца. Например, упомянутый вами Component. Даже не рефлектором, оригинальный код (он же у вас есть, надеюсь):

    /// <devdoc>
    ///  <para>
    ///    Disposes of the <see cref='System.ComponentModel.Component'/> 
    ///    .
    ///  </para> 
    /// </devdoc> 
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
    public void Dispose() { 
      Dispose(true);
      GC.SuppressFinalize(this);
    }
 
    /// <devdoc>
    ///  <para> 
    ///  Disposes all the resources associated with this component. 
    ///  If disposing is false then you must never touch any other
    ///  managed objects, as they may already be finalized. When 
    ///  in this state you should dispose any native resources
    ///  that you have a reference to.
    ///  </para>
    ///  <para> 
    ///  When disposing is true then you should dispose all data
    ///  and objects you have references to. The normal implementation 
    ///  of this method would look something like: 
    ///  </para>
    ///  <code> 
    ///  public void Dispose() {
    ///    Dispose(true);
    ///    GC.SuppressFinalize(this);
    ///  } 
    ///
    ///  protected virtual void Dispose(bool disposing) { 
    ///    if (disposing) { 
    ///      if (myobject != null) {
    ///        myobject.Dispose(); 
    ///        myobject = null;
    ///      }
    ///    }
    ///    if (myhandle != IntPtr.Zero) { 
    ///      NativeMethods.Release(myhandle);
    ///      myhandle = IntPtr.Zero; 
    ///    } 
    ///  }
    /// 
    ///  ~MyClass() {
    ///    Dispose(false);
    ///  }
    ///  </code> 
    ///  <para>
    ///  For base classes, you should never override the Finalier (~Class in C#) 
    ///  or the Dispose method that takes no arguments, rather you should 
    ///  always override the Dispose method that takes a bool.
    ///  </para> 
    ///  <code>
    ///  protected override void Dispose(bool disposing) {
    ///    if (disposing) {
    ///      if (myobject != null) { 
    ///        myobject.Dispose();
    ///        myobject = null; 
    ///      } 
    ///    }
    ///    if (myhandle != IntPtr.Zero) { 
    ///      NativeMethods.Release(myhandle);
    ///      myhandle = IntPtr.Zero;
    ///    }
    ///    base.Dispose(disposing); 
    ///  }
    ///  </code> 
    /// </devdoc> 
    protected virtual void Dispose(bool disposing) {
      if (disposing) { 
        lock(this) {
          if (site != null && site.Container != null) {
            site.Container.Remove(this);
          } 
          if (events != null) {
            EventHandler handler = (EventHandler)events[EventDisposed]; 
            if (handler != null) handler(this, EventArgs.Empty); 
          }
        } 
      }
    }

Вот унаследованный от другого IDisposable SqlTransaction:

```
    protected override void Dispose(bool disposing) { 
      if (disposing) {
        SNIHandle bestEffortCleanupTarget = null; 
        RuntimeHelpers.PrepareConstrainedRegions();
        try {
#if DEBUG
          TdsParser.ReliabilitySection tdsReliabilitySection = new TdsParser.ReliabilitySection(); 

          RuntimeHelpers.PrepareConstrainedRegions(); 
          try { 
            tdsReliabilitySection.Start();
#else 
          {
#endif //DEBUG
            bestEffortCleanupTarget = SqlInternalConnection.GetBestEffortCleanupTarget(_connection);
            if (!IsZombied && !IsYukonPartialZombie) { 
              _internalTransaction.Dispose();
            } 
          } 
#if DEBUG
          finally { 
            tdsReliabilitySection.Stop();
          }
#endif //DEBUG
        } 
        catch (System.OutOfMemoryException e) {
          _connection.Abort(e); 
          throw; 
        }
        catch (System.StackOverflowException e) { 
          _connection.Abort(e);
          throw;
        }
        catch (System.Threading.ThreadAbortException e) { 
          _connection.Abort(e);
          SqlInternalConnection.BestEffortCleanup(bestEffortCleanupTarget); 
          throw; 
        }
      } 
      base.Dispose(disposing);
    }
```

Видите дурацкие проверки, вызовы диспоуза для полей?
DataView:

    protected override void Dispose(bool disposing) { 
      if (disposing) {
      	Close();
      }
      base.Dispose(disposing); 
    }


System.Timers.Timer:

    /// <internalonly/> 
    /// <devdoc>
    /// </devdoc> 
    protected override void Dispose(bool disposing) {
      Close();
      this.disposed = true;
      base.Dispose(disposing); 
    }


Срочно пишите в коннект чтобы поубирали проверки.

23 августа 2011 г. 17:25

Модератор

> Нет, я закомментил Clear в Clear потому что это было проще, чем копипастить foreach в диспоуз, для проверки утверждения "или только foreach из Clear тут, почти не влияет на результаты."

как-то витиевато выражаетесь :)
и для чего постить код про работу с неуправялемыми ресурсами.

напоминаю: изанчально тема была про работу OleDbDataAdapter. вот эта тема
я привел пример-кальку с OleDbDataAdapter и т.д.
для чего вы флудите про неуправляемы ресурсы? может про COM-объекты еще пофлудите? :)

23 августа 2011 г. 17:38

На случай, если у вас нет нормальных исходников .NET, и вы мучаетесь с рефлектором. Вот сравнительный пример. Мой, с левыми проверками и вообще Clear. Ваш, без проверок и с вызовом Clear.

Запустите на своей машине и сравните результаты. Отпишитесь в этом топике с конкретными цифрами. Удивите меня, скажите что мой код работает медленее вашего. Найдите хоть кого-то со стороны, у кого код работает медленее вашего. Приведите рабочий пример, без скрытых багов. Или просто молчите.

```
  public partial class Form1 : Form
  {
    List<int[]> arrs = new List<int[]>();
    void LoadArray()
    {
      var arr = new int[100000];
      arrs.Add(arr);
    }
    public Form1()
    {
      var count = 500.0;
      Button b1 = new Button() { Parent = this, Text = "create -> dispose", Location = new Point(10, 10), Width = 200 };
      b1.Click += (s, e) =>
      {
        var sw = Stopwatch.StartNew();
        arrs.Clear();
        for (var i = 0; i < count; i++)
        {
          using (var c = new Collection2(0)) { }
          LoadArray();
        }
        MessageBox.Show(sw.ElapsedMilliseconds.ToString());
      };
      Button b2 = new Button() { Parent = this, Text = "create -> clear -> dispose", Location = new Point(10, b1.Bottom + 5), Width = 200 };
      b2.Click += (s, e) => // с Clear заметно быстрее
      {
        var sw = Stopwatch.StartNew();
        arrs.Clear();
        for (var i = 0; i < count; i++)
        {
          using (var c = new Collection(0))
          {
            c.Clear();
          }
          LoadArray();
        }
        MessageBox.Show(sw.ElapsedMilliseconds.ToString());
      };
    }

    class Entity2 : IDisposable
    {
      public int Id { get; private set; }
      public Entity2(int id) { this.Id = id; }
      ~Entity2()
      {
        this.Dispose(false);
      }
      private bool disposed = false;

      //Implement IDisposable.
      public void Dispose()
      {
        Dispose(true);
        GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
        if (!disposed)
        {
          if (disposing)
          {
            // Free other state (managed objects).
          }
          // Free your own state (unmanaged objects).
          // Set large fields to null.
          disposed = true;
        }
      }
    }

    class Collection2 : Entity2
    {
      public IEnumerable<Entity2> Items { get; private set; }
      public Collection2(int id)
        : base(id)
      {
        var list = new List<Entity2>();
        for (int i = 0; i < 10000; i++) list.Add(new Entity2(i));
        this.Items = list;
      }

      private bool disposed = false;

      protected override void Dispose(bool disposing)
      {
        if (!disposed)
        {
          if (disposing)
          {
            foreach (var itm in this.Items)
              itm.Dispose();
          }

          disposed = true;
        }

        base.Dispose(disposing);
      }
    }

    class Entity : IDisposable
    {
      public int Id { get; private set; }
      public Entity(int id) { this.Id = id; }
      ~Entity() { }
      public virtual void Dispose() { GC.SuppressFinalize(this); }
    }
    class Collection : Entity
    {
      public IEnumerable<Entity> Items { get; private set; }
      public Collection(int id)
        : base(id)
      {
        var list = new List<Entity>();
        for (int i = 0; i < 10000; i++) list.Add(new Entity(i));
        this.Items = list;
      }
      public void Clear()
      {
        foreach (var itm in this.Items) itm.Dispose();
        ((List<Entity>)this.Items).Clear();
      }
      public override void Dispose()
      {
        this.Items = null;
        base.Dispose();
      }
    }
  }
```

23 августа 2011 г. 17:39

Модератор

IDisposable - это интерфейс для освобождения неуправляемых ресурсов. Иногда еще для финтов типа Scope. Уж это то модератор должен знать. Это же основы.

Dispose Выполняет определяемые приложением задачи, связанные с высвобождением или сбросом неуправляемых ресурсов.

Нет неуправляемых ресурсов - IDisposable и финализаторы не нужны. Нет финализаторов - нет проблемы.

Запустите у себя сравнительный пример, из соседнего поста. Там мой код вообще без Clear и ваш Clear. Напрягиесь, я же ваш пример удостужился запустить,

23 августа 2011 г. 17:44

Модератор

```
// код из System.Data.Common.DataAdapter, с него сделана калька: метод Collection.Dispose в моем примере 

protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        this._tableMappings = null;
    }
    base.Dispose(disposing);
}

 

// ваш код.


protected override void Dispose(bool disposing)
{
    if (!disposed)
    {
        if (disposing)
        {
            foreach (var itm in this.Items)
                itm.Dispose();
        }

        disposed = true;
    }

    base.Dispose(disposing);
}
```

будете продолжать упорствовать?  :) или одумаетесь? только прошу не правьте чужие сообщения. это как минимум некрасиво.

23 августа 2011 г. 17:51

Ок, давайте я попробую указать на ключевую разницу между DataAdapter и вашим Collection.

Класс DataTableMapping - не IDisposable. Владеющий им DataAdapter не должен идти по коллекции и вызывать DataTableMapping.Dispose.

Класс Entity в вашем примере - IDisposable, да еще и с финализатором. Владеющий им класс Collection должен идти по коллекции и вызывать Dispose.

Сделайте полную кальку, замените у себя Entity на какой-нибудь не-IDisposable MyDataTableMapping - и проблема с производительностью так же исчезнет.

Просьба прочитать внимательно, а не просто кричать "я делал кальку". Вы сделали кальку с багом.

23 августа 2011 г. 18:02

Модератор

> IDisposable - это интерфейс для освобождения неуправляемых ресурсов. Иногда еще для финтов типа Scope.

это вы прочли где-то в книжке. а теперь загляните в релизацию System.Data.Common.DataAdapter

23 августа 2011 г. 18:04

Заглянул. DataAdapter и его наследники используют неуправляемые ресурсы. Ваша очередь заглядывать.

Чтобы совсем не обмануть - на самом деле DataAdater это компонент, и IDisposable там чисто для борьбы с утечками памяти.

23 августа 2011 г. 18:06

Модератор

> Ок, давайте я попробую указать на ключевую разницу между DataAdapter и вашим Collection.
Класс DataTableMapping - не IDisposable.

вы хоть поняли что написали?
посмотрите на пример утром. у вас случайно не корпоративчик был? ;)

23 августа 2011 г. 18:07

Действительно, зачем приводить агрументы, если можно просто нахамить. Тема закрыта.

23 августа 2011 г. 18:15
Модератор

> я процитирую Илью Сазонова: ... Вы не имеете права осуществлять цензуру чужих мнений

мы в теме №2, а вы загляните в первоисточник, тему №1. вы там удалили  чужие сообщения с примерами, которые опровергли ваше утверждение.

> все Entity остаются висеть в очереди на финализацию

и что?
есть managed heap, есть finalizable queue и еще есть freachable queue.
в какой из очередей они висят? и как вы это определили? опишите подробней. вы же должны помогать другим понять что к чему.

только не надо удалять это сообщение ;)

27 августа 2011 г. 10:52

> .... и дал ссылку на документацию.

для чего давать ссылку на документацию, если у всех есть VS. в ней можно проверить все что требуется.
скажите, как в VS проверить количество объектов, готовых к финализации?

только не надо удалять это сообщение ;)

27 августа 2011 г. 11:00

Я, кстати, не удалял ваши сообщения с кодом. И в том топике - тоже, только поддерживал его закрытым, в состоянии, в которое его привел другой модератор (не я). Он удалил и мои сообщения, кстати. Надеюсь, это смягчит вашу злобу. Deletion Log показывается не весь, только последние несколько сообщений. 

Вы вынесли обсуждение вашего опровергающего примера в эту тему, так зачем было плодить дубли еще и в старом топике? Или отдельная тема, со ссылкой из старой. Или код в старой теме, но никакого нового обсуждения.

Чтобы пример был опровергающим, нужно сначала показать, что именно Clear влияет на показатели. Это, кстати, очень легко опровергнуть. Удалите строчку `((List<Entity>)this.Items).Clear()`, и все равно сохранится перекос в сторону второго варианта. Возможно, разница даже станет больше. Это первое, что я сделал, увидев разницу в вашем примере (у меня та же разница, что у вас, если не под отладкой).

Т.е. разницу дает что-то еще, а не вызов где-то в коде Array.Clear. Вызова нет - а разница та же. Убедительно?

Насчет финализации - в очереди F-reachable, естественно. К сожалению, стантартный sos может показать только очередь объектов, готовых к финализации - недостижимых, и находящихся в Finalize Queue (команда !FinalizeQueue  -allReady).

Опять же, к сожалению, SuppressFinalize не убирает объект из очереди финализации физически, а просто сбрасывает флаг (об этом сказано в статье у Рихтера, да и сами сможете просмотреть ниже, добавив команды !finq по желанию). А sosex не работает в студии (по крайней мере у меня).

1. Выкачайте и поставьте windbg, нужной битности. (он входит в debugging tools for windows, те входят в windows sdk)

2. Выкачайте и распакуйте sosex последней версии: http://www.stevestechspot.com/

3. Запустите windbg, под админом.

4. Пропишите путь к отладочным символам: в File/Symbol File Path: SRV*d:\symbols*http://msdl.microsoft.com/download/symbols

5. Проверьте битость своего приложения Поставьте Debugger.Break() вместо/после LoadArray. Запустите без отладки.

6. Приаатачьтесь к процессу (File/Attach to a Process) (или сразу запустите его через File/Open Executable)

7. Дебаггер сразу же остановится. Загрузите sosex: .load путь\к\sosex.dll

8. Продолжите выполнение: g, Enter. Или F5.

9. Нажмите на кнопку в приложении, для варианта в котором хочется посмотреть очередь.

10. Пару раз нажмите F5, чтобы сборщик мусора успел...собрать мусор. В время сбора он проверят необходимость вызова финализатора, по наличию записи в finq, и флагу suppress finalize, и добавляет объек в F-reachable. В первой итерации есть шанс ничего не увидеть.

11. Посмотрите очередь F-reachable: !frq

Повторите для другой кнопки, желательно, с рестартом примера.

В первом варианте, без вызова dispose для всех entity в collection - в очереди тысячи объектов.

В втором, с вызовом Dispose в цикле для всeх Entity, или в моей реализации - в очереди пусто.

Если что-то не получилось - пишите, помогу.

27 августа 2011 г. 18:04

Модератор

> Я, кстати, не удалял ваши сообщения с кодом.

ага, целых три раза не удалял :)
Удалено PashaPash Microsoft Community Contributor, Модератор 7 ч. 48 мин. назад при всем уважении...  
Удалено PashaPash Microsoft Community Contributor, Модератор 7 ч. 32 мин. назад у двух модераторов, прошу заметить. Выше есть вывод этого кода на моей машине, он противоречит вашим утверждениям.  
Удалено PashaPash Microsoft Community Contributor, Модератор 2 ч. 49 мин. назад Sample shows wrong results due buggy implementation of Dispose/Finalize.  

это видно модераторам в теме http://social.msdn.microsoft.com/Forums/ru-RU/programminglanguageru/thread/aa78a422-c06f-4736-af43-20d048ee7a56 

P.S.
не надо удалять/редактировать это сообщение

27 августа 2011 г. 18:43

> К сожалению, стантартный sos может показать только очередь объектов, готовых к финализации

это не так. не надо вводить посетителей форума в заблуждение.
с помощью sos можно посмотреть freachable queue. как? я думаю вы сами найдете ответ.
как найдете, отпишитесь здесь. вы же должны помогать посетителям форума.

P.S.
не надо удалять/редактировать это сообщение

27 августа 2011 г. 18:52

Так покажите как. Я sos-ом давно пользовался последний раз, мало ли что там в 4.0 появилось. Заодно покажите почему я ошибся, а вы правы. Или скажите, что эксперимент подтвердил что я прав, и Clear ни при чем. А необоснованные наезды, пожалуйста, оставьте при себе.

Раз вы научились просматривать F-Reachable - вы можете прямо ответить на вопрос:

- да, дело в забивании очереди, или

- да, дело в clear. при удалении clear - дело а чем то другом <в чем именно>

А то сейчас у вас sosex в студии заработает, и меня опять во лжи обвинят. Не отклоняйтесь от темы, пожалуйста.

Изменено PashaPash Moderator 27 августа 2011 г. 19:12

27 августа 2011 г. 19:00

Модератор

Про ограничение на показ истории удаления я уже написал. 

Вас не устравивает то, что ваше сообщение не продублировано в двух топиках? Напишите на это жалобу, мол Pashapash удаляет дубликаты. Если кто-то из администрации скажет - ты не прав, нужно при выносе обсуждения оставлять дубликат его в старой теме - с удовольствием восстановлю.

27 августа 2011 г. 19:04

Модератор

> Так покажите как.

вам? для чего? это вы по правилам форума должны помогать.

> Заодно покажите почему я ошибся, а вы правы.

так вы, к сожалению, опять удалите мое сообщение.
или другой модератор удалит по вашей просьбе. и пометит пример с кодом как агрессивный :)
уже проходили.

ждем от вас ответ как использовать sos.
есть отдельная тема
http://social.msdn.microsoft.com/Forums/ru-RU/vsru/thread/7cdf2c79-cda1-4cb8-b807-2a943bfab8fd

P.S.
не надо удалять/редактировать это сообщение.

27 августа 2011 г. 19:10

> Напишите на это жалобу, мол Pashapash удаляет дубликаты.

куда писать? :) в ООО Майкрософт Рус? и не надо опять уводить тему в сторону.
тема простая: деструктор и SuppressFinalize ускоряют работу системы.

но вы утверждаете, что это не так :)

P.S.
не надо удалять/редактировать это сообщение. 

Изменено Malobukv 27 августа 2011 г. 19:17

27 августа 2011 г. 19:11

Вас же кто-то назначил модератором недавно? Вы так и не сказали, кто. Ему и пишите. Я писал Ивану Воронцову и Дмитрию Аболмасову, помогло. Если хоть кто-то, кроме вас, попросит меня восстановить дубликат начала этого топика в старой теме - я с удовольствием это сделаю.

27 августа 2011 г. 19:15

Модератор

> Вас же кто-то назначил модератором недавно?

для меня статус модератора не важен. и назначили давно.

восстановите пример в теме http://social.msdn.microsoft.com/Forums/ru-RU/programminglanguageru/thread/aa78a422-c06f-4736-af43-20d048ee7a56  - вы его там удалили. три раза.

P.S.
не надо удалять/редактировать это сообщение.

27 августа 2011 г. 19:19

Пожалуйста, перестаньте спамить про злых модераторов, удаление и редактирование ваших сообщений. Это просьба от меня как от модератора, а не от участника спора. Я даже не буду ничего удалять. Просто помечу ваш пост как оффтоп. Или другой пользователь пометит ваш пост как оффтоп.

Хорошая система. Вы обвиняете меня в том, что я умышленно скрываю секретный способ посмотреть freachable через sos. Но при этом я, такой вот глупый и нерациональный, трачу время и привожу подробную инструкцию как просмотреть freachable через sosex. Это обман в стиле "я обманул таксиста, заплатил, а сам не поехал". Возможно я не знаю всех деталей sos, пользовался действительно пару лет назад. Но не считайте меня идиотом, пожалуйста.
Это технический форум. Приведите доказательство. Код, ссылка на документацию, что угодно. Иначе скажи те "не знаю" или молчите.

27 августа 2011 г. 19:24

Модератор

> вам? для чего? это вы по правилам форума должны помогать.

я вам помог - рассказал про sos. указал на ошибочный вывод, который вы сделали из вашего примера. Подождем ответов с новом топике. Мне самому интересно - а вдруг можно.

27 августа 2011 г. 19:26

Модератор

Тогда пишите людям из моего списка.

Оукей, восстановил. Оставил пояснение и линк на эту тему. Справедливо?

27 августа 2011 г. 19:30

Модератор

> Приведите доказательство. Код, ссылка на документацию, что угодно.

ваше описание как использовать windbg - это не есть доказательство.
доказательство это ответ дебагера, например, такой:

```
SyncBlocks to be cleaned up: 0
  MTA Interfaces to be released: 0
  STA Interfaces to be released: 0
  ----------------------------------
  generation 0 has 0 finalizable objects (003d5bac->003d5bac)
  generation 1 has 3 finalizable objects (003d5ba0->003d5bac)
  generation 2 has 0 finalizable objects (003d5ba0->003d5ba0)
  Ready for finalization 1 objects (003d5bac->003d5bb0) 
...

!Threads
 PreEmptive   GC Alloc                Lock
 ID  OSID ThreadOBJ    State GC           Context       Domain   Count APT Exception
 8504    1  2138 00465828     86028 Enabled  01bec234:01bee004 0045ead8     0 STA
 10392    2  2898 00467348      b228 Enabled  00000000:00000000 0045ead8     0 MTA (Finalizer)
...
```

так что будьте добры, как специалист, поделитесь знаниями. если сами не знаете, то спросите у коллег.
обратитесь к сотрудникам. они же ваши сообщения читают, как вы сами выше сказали, обязательно ответят.

тема очень серьезная.
ведь код, который по-вашему с багом есть в .NET Framework.

27 августа 2011 г. 19:32

Ок, вот ответ дебаггера, на 6-й итерации:

Без вызова Dispose для вложенных Entity:

```
0:000> !finalizequeue
SyncBlocks to be cleaned up: 0
MTA Interfaces to be released: 0
STA Interfaces to be released: 0
----------------------------------
generation 0 has 7951 finalizable objects (004399cc->00441608)
generation 1 has 12053 finalizable objects (0042dd78->004399cc)
generation 2 has 44 finalizable objects (0042dcc8->0042dd78)
Ready for finalization 15950 objects (00441608->00450f40)

С вызовом Dispose (а в нем - SuppressFinalize) для вложенных Entity и Clear для списка:
0:000> !FinalizeQueue 
SyncBlocks to be cleaned up: 0
MTA Interfaces to be released: 0
STA Interfaces to be released: 0
----------------------------------
generation 0 has 6075 finalizable objects (051a3e54->051a9d40)
generation 1 has 3927 finalizable objects (051a00f8->051a3e54)
generation 2 has 44 finalizable objects (051a0048->051a00f8)
Ready for finalization 0 objects (051a9d40->051a9d40)
Просто с вызвовом Dispose (а в нем - SuppressFinalize) для вложенных Entity, без Clear

0:000> !finalizequeue
SyncBlocks to be cleaned up: 0
MTA Interfaces to be released: 0
STA Interfaces to be released: 0
----------------------------------
generation 0 has 22840 finalizable objects (004c8268->004de748)
generation 1 has 17165 finalizable objects (004b7634->004c8268)
generation 2 has 45 finalizable objects (004b7580->004b7634)
Ready for finalization 0 objects (004de748->004de748)
```

Мой вывод - Clear не нужен.
Баг в .net запостите в коннект.

А вы нашли объяснение, почему удаление непосредственно List/Array.Clear не изменяет результаты? Все это обсуждение про финализаторы может затянуться (уже затянулось). Способ с windbg/sosex был удобен лично мне - потому что все было настроено, символы выкачаны, команды я примерно помнил - осталось только отладить и проверить. Получили тот же результат, в студии с sos - отлично, рад за вас. Можете, кроме просмотра очереди финализации:

1. Удалить все-таки вызов Clear и объяснить отсутствие перемен еще чем-то.

2. Добавить трассировку в финализатор Entity, и проверить что в одном случае он часто вызывается, в втором - нет.

27 августа 2011 г. 20:33

Модератор

Кстати, если знаете как в sos посмотреть именно список Ready for finalization, а не просто count - скажите. Мало ли, добавили что-то в 4-ом дотнете. Честно признался что не знаю, спросил вас "как". Знаете - скажите. Не знаете - молчите. Очень простой принцип, ускоряет обсуждение в сотни тысяч раз. Спасибо за понимание.

Вы так усиленно уклоняетесь от прямого ответа "Clear ни при чем" .. вы действительно думаете что еще один пост не по теме что-то изменит? Будьте профессионалом, скажите "да, я был неправ". Я же сказал что "я не знаю новых фишек сос под .net 4" - и ничего, жив, даже ответ вам написал.

27 августа 2011 г. 20:43

Модератор

Malobukv, вы разобрались с проблемой? Если да - то отметьте, пожалуйста, ответы, которые решают проблему. Если нет - уточните вопрос, или смените тип на обсуждение. Спасибо.
 
27 августа 2011 г. 22:03

Модератор

> Оукей, восстановил. Оставил пояснение и линк на эту тему. Справедливо?

вот и замечательно. отвечаю здесь на ваше сообщение, потому что та тема заблокирована.

вы пишете: "Пример отличный, но удаление строки `((List<Entity>)this.Items).Clear();` дает тот же перекос в результатах."

отвечаю: в примере все имеет значение. если удалить еще что-нибудь, а потом еще, то вообще перестанет работать.
другими словами, если у машины удалить колесо, то будет перекос. не так ли? :)

28 августа 2011 г. 8:53

> я вам помог - рассказал про sos.

в теме http://social.msdn.microsoft.com/Forums/ru-RU/vsru/thread/7cdf2c79-cda1-4cb8-b807-2a943bfab8fd про sos
вообще НЕТ ответов. забыли отправить?

28 августа 2011 г. 8:57

----------------------------------------------

## Баг в .NET Framework 4.0

Источник: https://yandexwebcache.net/yandbtm?fmode=inject&tm=1712641413&tld=ru&lang=ru&la=1682147072&text=форумы+msdn+pashapash+malobukv+site%3Amicrosoft.com&url=https%3A//social.msdn.microsoft.com/Forums/windows/ru-RU/92b54cb5-b3b4-47aa-a70d-af5ceb261f67/104110721075-1074-net-framework-40%3Fforum%3Dprogramminglanguageru&l10n=ru&mime=html&sign=21b186783c262ce9be5e1da86d2f169d&keyno=0

Среда Visual Studio и языки программирования \> Языки программирования

ВНИМАНИЕ: заголовок темы и часть сообщения (см. ниже #2) использовались для доказательства "от обратного" того, что пример (ниже фрагмент в #1) также без бага. в итоге оппонент на англоязычном форуме признал, что в коде нет бага.
Эта тема касается всех, кто использует DataAdapter и другие реализации IDisposable.

1\. в следующем коде есть "баг в реализации Dispose/Finalize" (по утверждению mcc/msdn: здесь и здесь и для модераторов здесь)

```
class Entity : IDisposable
{
 public int Id { get; private set; }
 public Entity(int id) { this.Id = id; }
 ~Entity() { }
 public virtual void Dispose() { GC.SuppressFinalize(this); }
}
class Collection : Entity
{
 public IEnumerable Items { get; private set; }
 public Collection(int id)
 : base(id)
 {
 var list = new List();
 for (int i = 0; i < 10000; i++) list.Add(new Entity(i));
 this.Items = list;
 }
 public void Clear()
 {
 foreach (var itm in this.Items) itm.Dispose();
 ((List)this.Items).Clear();
 }
 public override void Dispose()
 {
 this.Items = null;
 base.Dispose();
 }
}
```

4\. возможные вопросы и ответы:

Q: почему в #2.Entity.~Entity() пусто?

A: в ситуации с Entity и его наследником Collection,
вызов Dispose(false) не нужен.

Q: почему в #2.Entity нет virtual void Dispose(bool)?

A: потому что в ситуации с Entity и его наследником, 
Dispose(bool) не нужен.

Q: может ли Dispose() быть виртуальным?

A: да, может. см.:
- в .NET Framework см. class HwndWrapper, System.Web.UI.Control, ...
- статья на msdn за 2009; автор - основатель и разработчик известных проектов.
- проект Microsoft
- более 5000 раз встречается в базе открытого кода

5\. вывод: код, который по утверждению mcc/msdn содержит
баг, есть в .NET Framework.
очевидно, что этот баг затрагивает очень многих.
о баге надо сообщить.  модераторы, пожалуйста,
создайте багрепорт на сайте microsoft connect и
отпишитесь в эту тему (не забудьте указать прямую
ссылку на страницу с багрепортом).
если это не баг, то получается, что не все ответы mcc/msdn
одинаково полезны ...
и чтобы впредь не было конфликтов между mcc/msdn и
другими участниками форумов msdn: надо запретить mcc/msdn удалять сообщения. (лучше просто переносить сообщения в какой-нибудь специальный форум, а
вместо оригинала оставлять ссылку на текст перенесенного сообщения).

2\. для наглядности давайте из примера слева оставим только то, что относится к Dispose/Finalize 

```
class Entity : IDisposable
{
 ~Entity() { }
 public virtual void Dispose() { GC.SuppressFinalize(this); }
}
class Collection : Entity
{
 public IEnumerable Items { get; private set; }
 public Collection()
 {
 this.Items = new List();
 }
 public override void Dispose()
 {
 this.Items = null;
 base.Dispose();
 }
}
```

3\. ниже код из .NET Framework: (скриншот с ILSpy)
видно, что он похож на код в #2:
в DataAdapter.Dispose обнуляется ссылка.
тоже самое сделано в #2ю.Collection.Dispose; и т.д.

Изменено Malobukv 29 августа 2011 г. 18:37 добавил "внимание:..."

28 августа 2011 г. 14:28

Пожайлуйста, не меняйте главное сообщение постфактум. С кодом как есть не будет проблем в том виде, в котором он есть. Но также код как есть не несет практически никакого смысла. И смысла наследования и реализации интерфейса IDisposable в нем тоже особо нет.

Подтвержение тому посты MVP с зарубежного форума:

1) О том, что реализация IDisposable своебразна и применима только к данному конкретному случаю

2) В коде PashaPash приведена верная реализация IDisposable в соответствии с документацией

3) Код Malobukv верен в том виде,в котором он есть, но в большом настоящем проекте, такая реализация скорее всего будет признана ошибочной.

Этот спор врядли кому из пользователей будет полезен, он больше введет их в заблуждение.  Топик закрыт.

Помечено в качестве ответа Abolmasov Dmitry 30 августа 2011 г. 11:12

30 августа 2011 г. 11:11

Вы сами можете создать багрепорт, если считаете нужным. Создайте, это совсем не сложно.

Можно уточняющий вопрос? Подставьте, пожалуйста, соответствующие классы из вашего примера:

```
Component -> ?

DataAdapter -> ?

DataTableMappingCollection -> ?

DataTableMapping -> ?
```

Я пока не вижу у вас в коде аналога DataTableMapping - обычного класса, без финализатора и IDisposable. И не вижу у вас в коде аналога DataTableMappingCollection - коллекции обычных, нефинализируемых объектов DataTableMapping. Приведите код в полное соответствие, пожалуйста.

То, что код "похож" - очень сомнительное обоснование. Код с багом всегда похож на код без бага. 

Если Collection владеет Disposable объектам Entity, то он должен вызывать у них Dispose в своем методе Dispose. Советую исправить его перед тем, как постить код в connect. Вы этого почему-то не делаете, уже третий топик подряд. Создаавать отдельный топик для обсуждения каждого бага в вашем коде, на мой взгляд, несколько неэфективно.

К коду из MSDN этого бага, естественно, нет - там DataTableMapping не IDisposable и не имеют финализаторов.

Отвечая на вопрос топика (как я его понял) - нет, в реализации Dispose/Finalize у DataAdatper бага нет. Ложная тревога.

28 августа 2011 г. 17:37

Модератор

> Я пока не вижу у вас в коде аналога DataTableMapping

в теме обсуждаем "баг в реализации Dispose/Finalize", а вы о чем? не надо уводить тему в сторону.

> То, что код "похож" - очень сомнительное обоснование. Код с багом всегда похож на код без бага.  

так вот и скажите где баг? как вы и собирались, цитирую "придется при всех ткнуть пальцем". ткните, не стесняйтесь. #2 перед вами.  где там баг? :) но если он там по-вашему есть, то пишите багрепорт в connect.

28 августа 2011 г. 17:49

Да я и не стесняюсь. Говорил вам уже раз 10 наверное. Если Collection владеет Disposable объектам Entity, то он должен вызывать у них Dispose в своем методе Dispose. Ссылки давал, "на пальцах" объяснял, код показывал. Давал вам исправленный вариант вашего номера 2.

 Пальцем: В #2 не хватает строчки перед this.Items = null:

    foreach (var item in this.Items) { item.Dispose(); } 

Постить баг в коннект их-за ошибки в вашем коде не вижу смысла. Connect - это багтрекер MS-а, а не ваших примеров.  Если вы не хотите понимать - я не обязан продолжать объяснять вам основы. За 4 дня можно было уж и прочитать хоть один мой ответ. 

28 августа 2011 г. 19:24

Модератор

> Да я и не стесняюсь.

это заметно по вашим сообщениям с фразами типа "мечу бисер перед свиньями" и т.д.  

но не уводите в сторону. посмотрите на свою реализацию Dispose с проверками в Entity и в Collection.
вы утверждали, что баг именно в моей реализации, потому что в ней нет проверок.
но проверок нет в .NET
так что пишите багрепорт.

28 августа 2011 г. 19:34

>> в теме обсуждаем "баг в реализации Dispose/Finalize", а вы о чем? не надо уводить тему в сторону.

В теме две реализации Dispose/Finalize:

ваша, с багом

в DataAdapeter,без.

Пожалуйста, если вы хотите обсуждать явную разницу между реализациями - переименуйте тему, и смените тип на обсуждение.

Если вы хотите обсуждать наличие якобы бага в стандартной реализации - оставьте в теме только ее, свою, с багом, перенесите в старую тему. Объяснение бага там уже есть, очень подробное.

Если вы хотите обсуждать свою реализацию - то, пожалуйста, уберите отрефлекченный .net. Или явно напишите что он приведен в теме "просто так" - он к вашему примеру отношения не имеет, по причинам, описанным вам выше. И в соседнем топике. И в соседнем топике в удаленном вами обсуждении.

Если у вас нет уточняющих вопросов по теме, пожалуйста, воздержитесь от флуда, фраз про коварных модераторов и перехода на личности.

Спасибо.

28 августа 2011 г. 19:36

Модератор

>> вы утверждали, что баг именно в моей реализации, потому что в ней нет проверок.

Не утверждал про проверки вообще ничего. Что я утверждал - написано одним сообщением выше. Проверки - потому что реализация взята из статьи по Dispose/Finalize. Еще "аргументы" есть?

Вы так забавно меня обвиняете, как будто я хочу скрыть баг в .net. Вы можете сказать что-то по теме: баг в .net 4.0? (а не в вашем коде).

28 августа 2011 г. 19:42

Модератор

> В теме две реализации Dispose/Finalize: ваша, с багом

так вот и скажите где в #2 баг? 10 строк кода. скажите в какой строке? вы же модератор и mcc - должны помогать.

вместо ответа вы выдаете массу слов/рассуждений не по делу.

P.S.
загляните в модераторский форум, где вы создали тему "Moderatorial War in russian forums - any way to stop it?". там уже есть ссылка на эту тему :) так что не удаляйте эту ветку, как предложил ваш коллега ulcer.
и не редактируйте чужие сообщения.

Изменено Malobukv 28 августа 2011 г. 19:50 добавил P.S.

28 августа 2011 г. 19:43

Зато я читаю чужие сообщения и не хамлю всем подряд без причины.

 В #2 не хватает строчки перед this.Items = null. Между 15 и 16 строками не хватает:
 
    foreach (var item in this.Items) { item.Dispose(); }  

вы на всякий случай перечитайте мои сообщения выше. Вдруг, ВНЕЗАПНО, я уже вам ответил. Есть еще что сказать? или я могу помечать этот пост ка кответ и закрывать тему?

28 августа 2011 г. 19:49

Модератор

> Зато я читаю чужие сообщения и не хамлю всем подряд без причины.

вы их не просто читаете, а УДАЛЯЕТЕ примеры с кодом.
про хамство: по-вашему ваша фраза "мечу бисер перед свиньями" и т.д. - это не хамство? :)

> В #2 не хватает строчки перед this.Items = null.

т.е. баг именно в том, что нет строки. но такой строки нет в DataAdapter
значит в DataAdapter - баг. пишите багрепорт.

> я могу помечать этот пост ка кответ и закрывать тему?

НЕТ

28 августа 2011 г. 19:55

В DataAdapter обнуляется ссылка на коллекцию DataTableMappingCollection, которая содержит объекты DataTableMapping, которые не являются IDisposable, поэтому и нет необходимости проходить по коллекции и вызывать для объектов метод Dispose.

IDisposable применяется для освобождения неуправляемых ресурсов, GC их не освобождает и ответственность за их освобождение лежит на разработчике.

Теперь представьте, что, как вы приводите во 2ом примере, вы создаете коллекцию их N объектов, которые оперируют с неуправляемыми ресурсами. А затем вы хотите освободить все ресурсы, занятые этой коллекцией - для этого нужно освободить ресурсы, занятые каждым объектом, следовательно вызывать метод Dispose каджого объекта. Простое присвоение коллекции к null этого не сделает и неуправляемые ресурсы останутся открытыми. В этом ваша ошибка.

29 августа 2011 г. 14:13

> Теперь представьте, что, как вы приводите во 2ом примере

спасибо за ответ. но вопрос/спор про 1й пример (#1 в первом сообщении).
а #2 - просто для наглядности, чтобы было легче сравнивать с кодом из .NET

внимание: мой оппонент (mcc/moderator) признал, что в коде нет бага: "ok, it's not a bug" http://social.msdn.microsoft.com/Forums/en-US/clr/thread/db86b52a-8fc9-4145-9a3e-d72cc72e8a3c/#00d7e213-a8c4-421f-8d75-4309473eb28a 

предыстория: изначально #1 был опубликован мной в теме в ответ на ошибочные слова mcc/moderator.
видимо mcc/moderator разозлился, и началось .... удаления кода, выпады в мой адрес, высмеивания, переход на личности, и т.д., вы и сами все видите в той теме и других темах (включая модераторские).
позже в теме код был восстановлен со словами: "Пример отличный..." (см. последнее сообщение).

в итоге он остается модератором, а меня лишили модераторских прав по его не вполне правдивой жалобе.
наверное посмотрели на его баллы и статус mcc.

29 августа 2011 г. 15:09

внимание: мой оппонент (mcc/moderator) признал, что в коде нет бага: "ok, it's not a bug"
Почему вы смотрите только на часть фразы? Полная фраза:

ok, it's not a bug. just serious performance issue.
что говорит о том, что "да - это не баг, а так, просто серьезная проблема c производительностью"

Вам же и другие пользователи написали о неправильной реализации IDisposable коллекции и о том что возникнет утечка памяти. Это применительно к общему случаю, конечно, конкретно в примере #1 таких проблем не возникнет. Но если кто-нибудь другой воспользуется вашей коллекцией для хранения настоящих IDisposable объектов (которые используют неуправляемые ресурсы) и вызовет Dispose без предварительного вызова Clear - то он получит проблему неосвобожденных ресурсов и утечку памяти.

29 августа 2011 г. 15:34

> Почему вы смотрите только на часть фразы?

потому что он утверждал, что в коде есть баг в реализации Dispose.

>  написали о неправильной реализации IDisposable коллекции и о том что возникнет утечка памяти. Это применительно к общему случаю, конечно, конкретно в примере #1 таких проблем не возникнет

так речь именно про код #1 "as is" т.е. "как есть" без расширений, изменений и т.д.
в нем нет бага. такая же "неправильная" реализация встречается в .NET
незачем было удалять пример и шуметь.

про утечку ... 
слова - это хорошо. но нужна точность. нужен рецепт/метод выявления.

> что говорит о том, что "да - это не баг, а так, просто серьезная проблема c производительностью"

а вы пример запускали? он из двух частей. запустите пример.

как видите Clear внутри using - против которого так яростно выступал(и) mcc/moderator - как-раз повышает проивзодительность.
почему? это другой вопрос.

код-пример просто показывает, что надо смотреть на реализацию. разве это плохо?
это же реклама .NET, у которого предусмотрена возможность просмотра кода сборок.

29 августа 2011 г. 15:57

Смотреть на реализацию - хорошо, но просто смотреть не достаточно, нужно еще понимать почему именно так это реализовано, а не иначе, что тяжело сделать имея только часть диссасемблированного кода .net, т.к. дальше может идти неуправляемый код, всякого рода обертки над ним и прочее..

Возможно ваш код из темы про using и работает быстрее, но такого рода микрооптимизации (ваша реализация IDisposable) в перспективе могут вылится в серьезные проблемы и головную боль при поиске и устранении их. Так что лучше придерживаться правильной принятой реализации IDisposable, чтобы потом не возникло подобных проблем.

29 августа 2011 г. 16:28

> Смотреть на реализацию - хорошо, но просто смотреть не достаточно ...

правильно. надо еще тестировать, тестировать и еще раз тестировать.
читать статьи/книги/ответы_на_форумах и проверять, а не верить на слово.

> Возможно ваш код из темы про using и работает быстрее, но такого рода микрооптимизации ...

вы же знаете, что при разработке высоконагруженных систем любая микрооптимизация - это хорошо.
и .NET-разработчики должны знать тонкости. вот я поделился. зря?

> ... в перспективе могут вылится в серьезные проблемы и головную боль при поиске и устранении их.

если код хорошо изолирован и покрыт юнит-тестами, то боли не будет.
сорри за повтор: мой код в #1 - это просто пример. частный случай. не более того.
он показывает как может быть. и больше ничего. и в коде нет агрессии и глупости, как утверждали mcc и не надо
было его удалять и т.д. и т.п. от mcc.

29 августа 2011 г. 16:43

>Возможно ваш код из темы про using и работает быстрее

> вы же знаете, что при разработке высоконагруженных систем любая микрооптимизация - это хорошо.
и .NET-разработчики должны знать тонкости. вот я поделился. зря?

Дело в том, что возможно он работает быстрее. А возможно - медленее. Причем медленее - с большей вероятностью. И ваше обоснование почему "быстрее" содержит ошибку.

> как видите Clear внутри using - против которого так яростно выступал(и) mcc/moderator - как-раз повышает проивзодительность.
почему? это другой вопрос.

Удаление вызова List.Clear оставляет разницу в производительности. Почему вы этого не хотите признать - другой вопрос. Вы же, надеюсь, не станете утверждать, что удаленный вызов List.Clear продолжает влиять на производительность?

29 августа 2011 г. 16:53

Модератор

Предлагаю перестать оффтопить, и вернуться к теме. Укажите, пожалуйста, в каком именно месте в коде DataAdapter есть баг?

29 августа 2011 г. 16:55

Модератор

> Дело в том, что возможно он работает быстрее. А возможно - медленее. Причем медленее - с большей вероятностью.

не надо гадать. про работу кода есть отдельная тема
http://social.msdn.microsoft.com/Forums/ru-RU/vsru/thread/2001ecc6-d241-439d-8b98-111b1eb67836

> Удаление вызова List.Clear оставляет разницу в производительности.

вообще-то, код - как песня, из него нельзя слова выкидывать.

29 августа 2011 г. 16:58

Question

На мой вгляд, художественная ценность вашего кода не является доказательством наличия бага в .net 4.0.

Непонятные мне обвинения в "гадании" - тоже. У вас есть что сказать по теме вопроса?

29 августа 2011 г. 17:51

Модератор

> На мой вгляд, художественная ценность вашего кода не является доказательством наличия бага в .net 4.0.

в .NET нет бага. доказательство того, что в моем коде "as is" нет бага было "от обратного" - есть такой метод.

вы признали, что бага в моем коде "as is" нет. 
в англоязычном форуме подтвердили http://social.msdn.microsoft.com/Forums/ru-RU/clr/thread/db86b52a-8fc9-4145-9a3e-d72cc72e8a3c

все рассуждения типа "а если там убрать, а если здесь добавить" не нужны.

29 августа 2011 г. 18:01

Пожайлуйста, не меняйте главное сообщение постфактум. С кодом как есть не будет проблем в том виде, в котором он есть. Но также код как есть не несет практически никакого смысла. И смысла наследования и реализации интерфейса IDisposable в нем тоже особо нет.

Подтвержение тому посты MVP с зарубежного форума:

1) О том, что реализация IDisposable своебразна и применима только к данному конкретному случаю

2) В коде PashaPash приведена верная реализация IDisposable в соответствии с документацией

3) Код Malobukv верен в том виде,в котором он есть, но в большом настоящем проекте, такая реализация скорее всего будет признана ошибочной.

Этот спор врядли кому из пользователей будет полезен, он больше введет их в заблуждение.  Топик закрыт.

Помечено в качестве ответа Abolmasov Dmitry 30 августа 2011 г. 11:12

30 августа 2011 г. 11:11

--------------------------------------------------

## Bug in .NET Framework 4.0? 

Источник: [https://social.msdn.microsoft.com/Forums/en-US/db86b52a-8fc9-4145-9a3e-d72cc72e8a3c/bug-in-net-framework-40?forum=clr](https://web.archive.org/web/20201021225442/https://social.msdn.microsoft.com/Forums/en-US/db86b52a-8fc9-4145-9a3e-d72cc72e8a3c/bug-in-net-framework-40?forum=clr)

.NET Framework \> Common Language Runtime Internals and Architecture

UPDATE: 30 Aug, 2011
there is no bug in .NET Framework. dispute is over.
my opponent - PashaPash - has acknowledged that in some cases Dispose into using block boosts performance and there is no bug in my first example.

Hello,

MCC claims that the following code has a bug.

```
class Entity : IDisposable
{
 ~Entity() { }
 public virtual void Dispose() { GC.SuppressFinalize(this); }
}
class Collection : Entity
{
 public IEnumerable Items { get; private set; }
 public Collection()
 {
 this.Items = new List();
 }
 public override void Dispose()
 {
 this.Items = null;
 base.Dispose();
 }
}
```

I believe there is no bug. Similar code we can find in .NET Framework. Samples are in theme (ru-ru).

Please, review code and please, do not let them (mcc|moderators: PashaPash and Ulcer) remove theme!

Thank you!

Edited by Malobukv Monday, August 29, 2011 10:58 PM

Sunday, August 28, 2011 8:30 PM

if the items in the collection implement IDisposable (e.g. SqlConnection) the code would leak. I cannot read russian but the code posted by MCCs fixed the problem.

This is not a bug in .Net, but a problem in your IDisposable implementation.

BTW is there a reason not using generics interfaces and collections now?

The following is signature, not part of post
Please mark the post answered your question as the answer, and mark other helpful posts as helpful, so they will appear differently to other users who are visiting your thread for the same problem.
Visual C++ MVP

Proposed as answer by ulcer Monday, August 29, 2011 11:32 AM  
Unproposed as answer by Malobukv Monday, August 29, 2011 12:12 PM  

Sunday, August 28, 2011 8:49 PM

> if the items in the collection implement IDisposable (e.g. SqlConnection) the code would leak.

there is no other code, just only what it is in the first message.
how can I know about the leak? would you please tell me.

> but the code posted by MCCs fixed the problem. 

do you mean code under #1 in theme?
it's not code of MCC. this is my code. but MCC claims about bug.

Sunday, August 28, 2011 9:03 PM

If the code above is the complete code of the class, then there is no leak - the List class does not need finalizing and there is no item added to the list .

But that would be an wasteful piece of code that does nothing beneficial to the end user. You won't see this in any project design, so the class would qualify as a design bug in real world. 

The following is signature, not part of post
Please mark the post answered your question as the answer, and mark other helpful posts as helpful, so they will appear differently to other users who are visiting your thread for the same problem.
Visual C++ MVP

Sunday, August 28, 2011 9:12 PM

Your implementation of IDisposable is incorrect.  It can cause a bug due to the virtual nature of how you have this implemented.

In this case, there actually is no reason to implement IDisposable.  However, if you decide to implement it, I would recommend doing it correctly.

For details on how (and when!) to do this correctly, see: http://reedcopsey.com/series/idisposable/

Reed Copsey, Jr. - http://reedcopsey.com
If a post answers your question, please click "Mark As Answer" on that post and "Mark as Helpful".

Sunday, August 28, 2011 9:17 PM

thanks to all for answers. sorry fo my english.
there is other example with special behavior.
please, would you comment, why Clear makes execution more faster?
and if there is a leak of memory, how i could know about?

sorry, no formating. editor "eats" some characters

```
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication12
{
public partial class Form1 : Form
{
List<int[]> arrs = new List<int[]>();
void LoadArray()
{
var arr = new int[100000];
arrs.Add(arr);
}
public Form1()
{
var count = 500.0;
Button b1 = new Button() { Parent = this, Text = "create -> dispose", Location = new Point(10, 10), Width = 200 };
b1.Click += (s, e) =>
{
var sw = Stopwatch.StartNew();
arrs.Clear();
for (var i = 0; i < count; i++)
{
using (var c = new Collection(0)) { }
LoadArray();
}
System.Diagnostics.Trace.WriteLine(sw.ElapsedMilliseconds, b1.Text);
};
Button b2 = new Button() { Parent = this, Text = "create -> clear -> dispose", Location = new Point(10, b1.Bottom + 5), Width = 200 };
b2.Click += (s, e) => // с Clear заметно быстрее
{
var sw = Stopwatch.StartNew();
arrs.Clear();
for (var i = 0; i < count; i++)
{
using (var c = new Collection(0))
{
c.Clear();
}
LoadArray();
}
System.Diagnostics.Trace.WriteLine(sw.ElapsedMilliseconds, b2.Text);
};
}
class Entity : IDisposable
{
public int Id { get; private set; }
public Entity(int id) { this.Id = id; }
~Entity() { }
public virtual void Dispose() { GC.SuppressFinalize(this); }
}
class Collection : Entity
{
public IEnumerable<Entity> Items { get; private set; }
public Collection(int id)
: base(id)
{
var list = new List<Entity>();
for (int i = 0; i < 10000; i++) list.Add(new Entity(i));
this.Items = list;
}
public void Clear()
{
foreach (var itm in this.Items) itm.Dispose();
((List<Entity>)this.Items).Clear();
}
public override void Dispose()
{
this.Items = null;
base.Dispose();
}
}
}
}
```

Sunday, August 28, 2011 9:31 PM

You'll know there's a memory leak if your application keeps doing the same thing, and instead of memory being constant over a period of time, the amount of memory will increase.

Remember, Dispose doesn't clean up memory, dispose is to be used release native resources. If you're class doesn't use P/Invoke to create any native objects, or has any disposable members, there's no need for the class to be disposable.

Sunday, August 28, 2011 11:48 PM

> there's a memory leak if your application keeps doing the same thing

how can I make sure that there is a memory leak in a code?
should I use a windows task manager? but it is not accurate enough.

> If you're class doesn't use P/Invoke to create any native objects, or has any disposable members, there's no need for the class to be disposable.

i'm use IDisposable with using(...) { ... }

Monday, August 29, 2011 7:06 AM

Malobukv, I already pointed you to a problem in the code above. It's not leaking memory or unmanaged resources (as there are not unmanaged resources used in this specific sample).

However, there will be a possible resources "leak" in real application if Entity holds unmanaged resources.

You sample has only a performance issue because you are not disposing (and SuppressFinalizing this.Items in Collection.Dispose(). Than causes an overload of F-Reachable queue. So the first sample will be definitely slower than second one, where you are explicitely calling Dispose/SuppressFinalizing.

I already answered you this question about 10 times in Russian forums during last 5 days. Why do you need another "your implementation of IDisposable is incorrect" to believe?

Monday, August 29, 2011 1:54 PM

PashaPash

> Malobukv, [...] in the code above. It's not leaking memory or unmanaged resources (as there are not unmanaged resources used inthis specific sample).

that is, you agree that the code "as is" has no bug.

REMINDER:  
the code without bug was removed 3 times by PashaPash from http://social.msdn.microsoft.com/Forums/ru-RU/programminglanguageru/thread/aa78a422-c06f-4736-af43-20d048ee7a56#c7a18c3d-8ed2-4010-83bb-2ad251ca2ba7  moderators could see history of deletions. and there is comments of PashaPash with profanity words.

Monday, August 29, 2011 2:34 PM

>> that is, you agree that the code "as is" has no bug.

ok, it's not a bug. just serious performance issue.

Monday, August 29, 2011 2:57 PM
 
Making it disposable will only make your performance worse, because it now will take more steps for the GC to clean the object up.
Monday, August 29, 2011 3:00 PM

> Making it disposable will only make your performance worse, because it now will take more steps for the GC to clean the object up

ok. thank you.
would you please explain why calling of Clear boosts performance?

```
using (var c = new Collection(0))
{
    c.Clear();
}
```

(see full code above in my post)

Monday, August 29, 2011 4:28 PM

Obviously, you are not only clearing collection, but also explicitly disposing Entities in your Collection.Clear implementation.

Clearing a collection itself has no significant impact on performance. Just try to remove the following line, and you will still have the same timings:

    ((List<Entity>)this.Items).Clear();

Monday, August 29, 2011 4:59 PM

> Just try to remove the following line, and you will still have the same timings:((List<Entity>)this.Items).Clear();

try to remove something more :)

could anybody explain performance boost in code "as is" with Clear, please.

Monday, August 29, 2011 5:05 PM

We're not phsycic. We could only make stabs in the dark as to why clearing the collection could have performance improvements, given the little sample of code shown here. Without knowing what's in the collection, there's no way we could figure out why you perceive a performance improvement when the code clears the collection.

Monday, August 29, 2011 6:45 PM

> given the little sample of code shown here

sorry for my english. code is above in this theme.
here is copy (without formating. because editor 'eats' some characters):

```
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication12
{
public partial class Form1 : Form
{
List<int[]> arrs = new List<int[]>();
void LoadArray()
{
var arr = new int[100000];
arrs.Add(arr);
}
public Form1()
{
var count = 500.0;
Button b1 = new Button() { Parent = this, Text = "create -> dispose", Location = new Point(10, 10), Width = 200 };
b1.Click += (s, e) =>
{
var sw = Stopwatch.StartNew();
arrs.Clear();
for (var i = 0; i < count; i++)
{
using (var c = new Collection(0)) { }
LoadArray();
}
System.Diagnostics.Trace.WriteLine(sw.ElapsedMilliseconds, b1.Text);
};
Button b2 = new Button() { Parent = this, Text = "create -> clear -> dispose", Location = new Point(10, b1.Bottom + 5), Width = 200 };
b2.Click += (s, e) => // с Clear заметно быстрее
{
var sw = Stopwatch.StartNew();
arrs.Clear();
for (var i = 0; i < count; i++)
{
using (var c = new Collection(0))
{
c.Clear();
}
LoadArray();
}
System.Diagnostics.Trace.WriteLine(sw.ElapsedMilliseconds, b2.Text);
};
}
class Entity : IDisposable
{
public int Id { get; private set; }
public Entity(int id) { this.Id = id; }
~Entity() { }
public virtual void Dispose() { GC.SuppressFinalize(this); }
}
class Collection : Entity
{
public IEnumerable<Entity> Items { get; private set; }
public Collection(int id)
: base(id)
{
var list = new List<Entity>();
for (int i = 0; i < 10000; i++) list.Add(new Entity(i));
this.Items = list;
}
public void Clear()
{
foreach (var itm in this.Items) itm.Dispose();
((List<Entity>)this.Items).Clear();
}
public override void Dispose()
{
this.Items = null;
base.Dispose();
}
}
}
}
```

Monday, August 29, 2011 7:16 PM

Ok, it will be even shorter: It's because you explicitly disposing Entities in your Collection.Clear implementation.

Monday, August 29, 2011 8:35 PM

PashaPash:
Ok, it will be even shorter: It's because you explicitly disposing Entities in your Collection.Clear implementation.

it's a part of answer. actualy into Entity.Dispose() is GC.SuppressFinalize - it's a key line.
so, as I told you, in some cases we could use Dispose within using block for performance.

nice that you understood.

we spent a lot of efforts since 23 August, 2011 (7 days before) for this theme.
please, never use profanity words in argue and do not delete examples.

thanks to all. we can close the theme.

Best regards,
Microsoft MVP C#, 2007-2011

Monday, August 29, 2011 10:33 PM

Just a note for other answerers - this topic is one of many that Malobukv spawned to proove that clearing DataAdapter.TableMapping just before disposing an adatper will speed up the process. The original code could be seen in the first post of the topic he linked in post before. You don't need know russian to check that. Don't loose you time, Malobukv definitely know that DataTableMapping has no finalizer defined, and that his code is totally unrelated (yes, I told him few times). And yes, he known that there is no bug in .net 4.0. And yes, I told him that clearing an array itself is unrelated to performance.
To "proove" that he is right, he typed some example with "Dispose" method named Clear, and broken Dispose. Then posted it in original topic stating that "Clearing a collection will speed up GC!, look on my code". And then he just used his moderator right to create illusion that his "sample" is somehow related to DataAdapter class. That's why this topic named Bug in .NET Framework 4.0.

Then, eventually, his moderator rights was revoked. I also hope that his MVP status (if any existis, as I still does not known a real name of this "MVP") will be reviewed.

Now he just translated an answer that I gave him 7 days before. Nice try, but anyone could use google translate to check that Malobukv is just lying: http://translate.google.com/translate?sl=auto&tl=en&u=http%3A%2F%2Fsocial.msdn.microsoft.com%2FForums%2Fru-RU%2Fvsru%2Fthread%2F2001ecc6-d241-439d-8b98-111b1eb67836. Check the post with translation starting with "A bit strange question".

But both his and my posts was reviewed by Russian forum owners/moderators. And you can see the result - he moved to English part just becase got a final warinng "stop spamming with you broken sample, it prooves nothing" in russian .net forums. The only purpose of this topic was, definitely, to get an answer "that two MCC|moderators are wrong" from one of the English forums answerers. Final post should say to all of you "look, I told that PashaPash has no basic knowledge of GC!!!", hoping that no one of you know russian, and will have no chance to check.

It's not a "Question Topic", just yet another spam "someone tell me that I right and bad Russian moderators are wrong topic". Sorry if you lost some time reading this topic from the beginning. Sorry for a long and probably offtopic answer. 

Monday, August 29, 2011 11:43 PM
