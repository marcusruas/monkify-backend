﻿using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;

namespace Monkify.Api.Controllers
{
    [Route("api/sessions")]
    [Produces("application/json")]
    public class DebugController : BaseController
    {
        public DebugController(IMediator mediador, IMessaging messaging, MonkifyDbContext context) : base(mediador, messaging)
        {
            _context = context;
        }

        private readonly MonkifyDbContext _context;

        [HttpPost("cadastrar-parametros")]
        public async Task<IActionResult> CadastrarParametros([FromBody] CorpoLol parametros)
        {
            var presetChoices = new List<PresetChoice>();

            if (!parametros.PresetChoices.IsNullOrEmpty())
            {
                presetChoices.AddRange(parametros.PresetChoices.Select(x => new PresetChoice(x)));
            }

            var novo = new SessionParameters()
            {
                AllowedCharacters = parametros.SessionCharacterType,
                Name = parametros.Name,
                RequiredAmount = parametros.RequiredAmount,
                MinimumNumberOfPlayers = parametros.MinimumNumberOfPlayers,
                ChoiceRequiredLength = parametros.ChoiceRequiredLength,
                AcceptDuplicatedCharacters = parametros.AcceptDuplicatedCharacters,
                Active = true,
                PresetChoices = presetChoices
            };

            await _context.AddAsync(novo);
            var result = await _context.SaveChangesAsync();

            return Ok(result > 0);
        }

        [HttpDelete("deletar-tudo")]
        public IActionResult DeletarTudo()
        {
            foreach (var item in _context.SessionParameters.ToList())
                _context.Remove(item);

            _context.SaveChanges();

            return Ok();
        }

        [HttpGet("buscar-todas-sessoes")]
        public IActionResult BuscarTudo()
        {
            return Ok(_context.Sessions.AsNoTracking().ToList());
        }
    }

    public class CorpoLol
    {
        public SessionCharacterType SessionCharacterType { get; set; }
        public string Name { get; set; }
        public decimal RequiredAmount { get; set; }
        public int MinimumNumberOfPlayers { get; set; }
        public int ChoiceRequiredLength { get; set; }
        public bool AcceptDuplicatedCharacters { get; set; }
        public bool Active { get; set; }
        public string[] PresetChoices { get; set; }
    }
}
