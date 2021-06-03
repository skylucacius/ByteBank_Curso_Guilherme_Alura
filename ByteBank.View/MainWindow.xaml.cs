using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using ByteBank.View.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        //private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        //{
        //    BtnProcessar.IsEnabled = false;

        //    _cts = new CancellationTokenSource();

        //    var contas = r_Repositorio.GetContaClientes();

        //    PgsProgresso.Maximum = contas.Count();

        //    LimparView();

        //    var inicio = DateTime.Now;

        //    BtnCancelar.IsEnabled = true;
        //    var progress = new Progress<String>(str =>
        //        PgsProgresso.Value++);
        //    //var byteBankProgress = new ByteBankProgress<String>(str =>
        //    //  PgsProgresso.Value++);


        //    try
        //    {
        //        var resultado = await ConsolidarContas(contas, progress, _cts.Token);

        //        var fim = DateTime.Now;
        //        AtualizarView(resultado, fim - inicio);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        TxtTempo.Text = "Operação cancelada pelo usuário";
        //    } 
        //    finally
        //    {
        //        BtnProcessar.IsEnabled = true;
        //        BtnCancelar.IsEnabled = false;
        //    }
        //}

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            var contas = r_Repositorio.GetContaClientes();

            var resultado = new List<string>();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            //foreach (var conta in contas)
            //{
            //    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
            //    resultado.Add(resultadoConta);
            //}

            Console.WriteLine("O número de contas é " + contas.Count());
            int cores = Environment.ProcessorCount;
            int accounts = contas.Count();
            Thread[] thread_vector = new Thread[cores];
            int i = 0;

            for (int core = 0; core < cores; core++)
            {
                thread_vector[core] = new Thread ( () =>
                {
                    IEnumerable<ContaCliente> contasAPegar = (core != cores - 1) ? contas.Skip(core * (accounts / cores)).Take(accounts / cores) : contas.Skip(core * (accounts / cores));
                    
                    foreach (ContaCliente _conta in contasAPegar)
                    {
                        var resultadoConta = r_Servico.ConsolidarMovimentacao(_conta);
                        resultado.Add(resultadoConta);
                            i++;
                    }
                });
                thread_vector[core].Start();
            }
            int thread_conclusions = 0;
            //esperar pela execução de todas as threads
            for (int core = 0; core < cores; core++)
            {
                if (!thread_vector[core].IsAlive)
                {
                    thread_conclusions++;
                    Thread.Sleep(250);
                }

                if (core == cores - 1 & thread_conclusions != cores - 1)
                {
                    core = 0;
                    thread_conclusions = 0;
                }
                else
                if (core == cores - 1 & thread_conclusions == cores - 1) 
                {
                    break;
                }
            }
            
            Console.WriteLine("A quantidade de registros iterados foi de: " + i);

            var fim = DateTime.Now;

            AtualizarView(resultado, fim - inicio);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;
            _cts.Cancel();
        }

        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas, IProgress<string> reportadorDeProgresso, CancellationToken ct)
        {
            var tasks = contas.Select(conta =>
                Task.Factory.StartNew(() =>
                {
                    ct.ThrowIfCancellationRequested();

                    var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta, ct);

                    reportadorDeProgresso.Report(resultadoConsolidacao);

                    ct.ThrowIfCancellationRequested();
                    return resultadoConsolidacao;
                }, ct)
            );

            return await Task.WhenAll(tasks);
        }

        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
            PgsProgresso.Value = 0;
        }

        private void AtualizarView(IEnumerable<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}
